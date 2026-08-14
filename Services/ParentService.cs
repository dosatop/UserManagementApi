using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.Parents;
using UserManagementApi.Models;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

public class ParentService : IParentService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserManagementService _userManagementService;

    public ParentService(
        ApplicationDbContext context,
        IUserManagementService userManagementService)
    {
        _context = context;
        _userManagementService = userManagementService;
    }

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
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SchoolId = schoolId
        };

        _context.Parents.Add(parent);

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                parent.Id,
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
                x.Id,
                x.UserId,
                FullName = x.User.FullName,
                Email = x.User.Email,
                PhoneNumber = x.User.PhoneNumber,

                ChildrenCount = x.Children.Count()
            })
            .ToListAsync();
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
                x.Id == parentId &&
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
                ParentId = parent.Id,
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