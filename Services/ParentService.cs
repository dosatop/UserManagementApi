using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.Parents;
using UserManagementApi.Models;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

public class ParentService(
    ApplicationDbContext context,
    IUserManagementService userManagementService) : IParentService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IUserManagementService _userManagementService = userManagementService;

    public async Task<(bool Success, object? Data, string? Error)>
        CreateParentAsync(
            Guid schoolId,
            CreateParentRequest request)
    {
        var school = await _context.Schools
            .FirstOrDefaultAsync(x => x.Id == schoolId);

        if (school == null)
        {
            return (
                false,
                null,
                "School not found."
            );
        }

        var userResult =
            await _userManagementService.CreateUserAsync(
                request.FullName,
                request.Email,
                request.Password,
                 request.PhoneNumber,
                null,
                                Roles.Parent);

        if (!userResult.Success)
        {
            return (
                false,
                null,
                userResult.Error
            );
        }

        var user = userResult.User!;

        user.PhoneNumber = request.PhoneNumber;

        var parent = new Parent
        {
            ParentId = Guid.NewGuid(),
            UserId = user.Id,
            SchoolId = schoolId
        };

        _context.Parents.Add(parent);

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                parent.ParentId,
                UserId = user.Id,
                user.FullName,
                user.Email,
                user.PhoneNumber,
                SchoolId = school.Id,
                SchoolName = school.Name
            },
            null
        );
    }

    public async Task<IEnumerable<object>>
        GetParentsAsync(Guid schoolId)
    {
        return await _context.Parents
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .Select(x => new
            {
                x.ParentId,
                x.UserId,
                FullName = x.User.FullName,
                Email = x.User.Email,
                PhoneNumber = x.User.PhoneNumber,

                ChildrenCount = x.Children.Count()
            })
            .ToListAsync();
    }


    public async Task<(bool Success, object? Data, string? Error)>
        GetParentAsync(
            Guid schoolId,
            Guid parentId)
    {
        var parent = await _context.Parents
            .AsNoTracking()
            .Where(x =>
                x.ParentId == parentId &&
                x.SchoolId == schoolId)
            .Select(x => new
            {
                x.ParentId,
                x.UserId,

                FullName = x.User.FullName,
                Email = x.User.Email,
                PhoneNumber = x.User.PhoneNumber,

                SchoolId = x.SchoolId,

                Children = x.Children
                    .Select(ps => new
                    {
                        StudentId = ps.Student.Id,
                        StudentNumber = ps.Student.StudentNumber,
                        StudentName = ps.Student.User.FullName,
                        Email = ps.Student.User.Email,
                        ClassId = ps.Student.ClassId,
                        ClassName = ps.Student.Class.Name
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (parent == null)
        {
            return (
                false,
                null,
                "Parent not found."
            );
        }

        return (
            true,
            parent,
            null
        );
    }

    public async Task<(bool Success, object? Data, string? Error)>
        UpdateParentAsync(
            Guid schoolId,
            Guid parentId,
            UpdateParentRequest request)
    {
        var parent = await _context.Parents
            .Include(x => x.User)
            .FirstOrDefaultAsync(x =>
                x.ParentId == parentId &&
                x.SchoolId == schoolId);

        if (parent == null)
        {
            return (
                false,
                null,
                "Parent not found."
            );
        }

        var fullName = request.FullName?.Trim();
        var email = request.Email?.Trim();
        var phoneNumber = request.PhoneNumber?.Trim();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return (
                false,
                null,
                "Full name is required."
            );
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return (
                false,
                null,
                "Email is required."
            );
        }

        // Check if another user already uses this email
        var emailExists = await _context.Users
            .AnyAsync(x =>
                x.Id != parent.UserId &&
                x.Email != null &&
                x.Email.ToLower() == email.ToLower());

        if (emailExists)
        {
            return (
                false,
                null,
                "A user with this email already exists."
            );
        }

        parent.User.FullName = fullName;
        parent.User.Email = email;
        parent.User.UserName = email;
        parent.User.PhoneNumber = phoneNumber;

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                parent.ParentId,
                parent.UserId,
                parent.User.FullName,
                parent.User.Email,
                parent.User.PhoneNumber,
                parent.SchoolId
            },
            null
        );
    }

    public async Task<(bool Success, string? Error)>
        DeleteParentAsync(
            Guid schoolId,
            Guid parentId)
    {
        var parent = await _context.Parents
            .Include(x => x.User)
            .FirstOrDefaultAsync(x =>
                x.ParentId == parentId &&
                x.SchoolId == schoolId);

        if (parent == null)
        {
            return (
                false,
                "Parent not found."
            );
        }

        // Remove parent/student relationships
        var assignments = await _context.ParentStudents
            .Where(x => x.ParentId == parentId)
            .ToListAsync();

        if (assignments.Count > 0)
        {
            _context.ParentStudents.RemoveRange(assignments);
        }

        // Remove parent profile
        _context.Parents.Remove(parent);

        // Remove Identity user
        if (parent.User != null)
        {
            _context.Users.Remove(parent.User);
        }

        await _context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, object? Data, string? Error)>
   AssignStudentAsync(
       Guid schoolId,
       Guid parentId,
       Guid studentId)
    {
        var parent = await _context.Parents
            .Include(x => x.User)
            .FirstOrDefaultAsync(x =>
                x.ParentId == parentId &&
                x.SchoolId == schoolId);

        if (parent == null)
        {
            return (
                false,
                null,
                "Parent not found."
            );
        }

        var student = await _context.StudentProfiles
            .Include(x => x.User)
            .FirstOrDefaultAsync(x =>
                x.Id == studentId &&
                x.SchoolId == schoolId);

        if (student == null)
        {
            return (
                false,
                null,
                "Student not found."
            );
        }

        var alreadyAssigned =
            await _context.ParentStudents
                .AnyAsync(x =>
                    x.ParentId == parentId &&
                    x.StudentId == studentId);

        if (alreadyAssigned)
        {
            return (
                false,
                null,
                "Student is already linked to this parent."
            );
        }

        var parentStudent = new ParentStudent
        {
            ParentId = parentId,
            StudentId = studentId
        };

        _context.ParentStudents.Add(parentStudent);

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                ParentId = parent.ParentId,
                ParentName = parent.User.FullName,

                StudentId = student.Id,
                StudentName = student.User.FullName,
                student.StudentNumber
            },
            null
        );
    }

    public async Task<(bool Success, string? Error)>
        RemoveStudentAsync(
            Guid schoolId,
            Guid parentId,
            Guid studentId)
    {
        var assignment =
            await _context.ParentStudents
                .FirstOrDefaultAsync(x =>
                    x.ParentId == parentId &&
                    x.StudentId == studentId &&
                    x.Parent.SchoolId == schoolId);

        if (assignment == null)
        {
            return (
                false,
                "Student is not linked to this parent."
            );
        }

        _context.ParentStudents.Remove(assignment);

        await _context.SaveChangesAsync();

        return (true, null);
    }
}