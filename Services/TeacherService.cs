using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.Teachers;
using UserManagementApi.Models;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

public class TeacherService(
    ApplicationDbContext context,
    IUserManagementService userManagementService,
    UserManager<User> userManager) : ITeacherService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IUserManagementService _userManagementService = userManagementService;
    private readonly UserManager<User> _userManager = userManager;

    // ================================================================
    // CREATE TEACHER
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        CreateTeacherAsync(
            Guid schoolId,
            CreateTeacherRequest request)
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

        var employeeExists = await _context.Teachers
            .AnyAsync(x =>
                x.SchoolId == schoolId &&
                x.EmployeeNumber == request.EmployeeNumber);

        if (employeeExists)
        {
            return (
                false,
                null,
                "A teacher with this employee number already exists in this school."
            );
        }

        var userResult =
            await _userManagementService.CreateUserAsync(
                request.FullName,
                request.Email,
                request.Password,
                request.PhoneNumber,
                request.EmployeeNumber,
                Roles.Teacher);

        if (!userResult.Success)
        {
            return (
                false,
                null,
                userResult.Error
            );
        }

        var user = userResult.User!;

        var teacherProfile = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SchoolId = schoolId,
            PhoneNumber = user.PhoneNumber,
            EmployeeNumber = request.EmployeeNumber
        };

        _context.Teachers.Add(teacherProfile);

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                teacherId = teacherProfile.Id,
                userId = user.Id,
                fullName = user.FullName,
                email = user.Email,
                phoneNumber = user.PhoneNumber,
                employeeNumber = teacherProfile.EmployeeNumber,
                schoolId = school.Id,
                schoolName = school.Name
            },
            null
        );
    }

    // ================================================================
    // GET ALL TEACHERS
    // ================================================================

    public async Task<IEnumerable<object>> GetTeachersAsync(
        Guid schoolId)
    {
        return await _context.Teachers
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .Select(x => new
            {
                teacherId = x.Id,
                userId = x.UserId,
                employeeNumber = x.EmployeeNumber,
                fullName = x.User.FullName,
                email = x.User.Email,
                phoneNumber = x.User.PhoneNumber,
                schoolId = x.SchoolId
            })
            .ToListAsync();
    }

    // ================================================================
    // GET TEACHER BY ID
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetTeacherByIdAsync(
            Guid schoolId,
            Guid teacherId)
    {
        var teacher = await _context.Teachers
            .AsNoTracking()
            .Where(x =>
                x.Id == teacherId &&
                x.SchoolId == schoolId)
            .Select(x => new
            {
                teacherId = x.Id,
                userId = x.UserId,

                employeeNumber = x.EmployeeNumber,

                fullName = x.User.FullName,
                email = x.User.Email,
                phoneNumber = x.User.PhoneNumber,

                schoolId = x.SchoolId,
                schoolName = x.School.Name
            })
            .FirstOrDefaultAsync();

        if (teacher == null)
        {
            return (
                false,
                null,
                "Teacher not found."
            );
        }

        return (
            true,
            teacher,
            null
        );
    }

    // ================================================================
    // UPDATE TEACHER
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        UpdateTeacherAsync(
            Guid schoolId,
            Guid teacherId,
            UpdateTeacherRequest request)
    {
        var teacher = await _context.Teachers
            .Include(x => x.User)
            .FirstOrDefaultAsync(x =>
                x.Id == teacherId &&
                x.SchoolId == schoolId);

        if (teacher == null)
        {
            return (
                false,
                null,
                "Teacher not found."
            );
        }

        // ------------------------------------------------------------
        // Check employee number
        // ------------------------------------------------------------

        var employeeExists = await _context.Teachers
            .AnyAsync(x =>
                x.SchoolId == schoolId &&
                x.EmployeeNumber == request.EmployeeNumber &&
                x.Id != teacherId);

        if (employeeExists)
        {
            return (
                false,
                null,
                "Another teacher with this employee number already exists in this school."
            );
        }

        // ------------------------------------------------------------
        // Check email
        // ------------------------------------------------------------

        var existingUser = await _userManager
            .FindByEmailAsync(request.Email);

        if (existingUser != null &&
            existingUser.Id != teacher.UserId)
        {
            return (
                false,
                null,
                "Another user already has this email address."
            );
        }

        // ------------------------------------------------------------
        // Update User
        // ------------------------------------------------------------

        teacher.User.FullName = request.FullName;
        teacher.User.PhoneNumber = request.PhoneNumber;

        // ------------------------------------------------------------
        // Update Email
        // ------------------------------------------------------------

        if (teacher.User.Email != request.Email)
        {
            var emailResult = await _userManager
                .SetEmailAsync(
                    teacher.User,
                    request.Email);

            if (!emailResult.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    emailResult.Errors.Select(x => x.Description));

                return (
                    false,
                    null,
                    errors
                );
            }

            var usernameResult = await _userManager
                .SetUserNameAsync(
                    teacher.User,
                    request.Email);

            if (!usernameResult.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    usernameResult.Errors.Select(x => x.Description));

                return (
                    false,
                    null,
                    errors
                );
            }
        }

        // ------------------------------------------------------------
        // Update Teacher profile
        // ------------------------------------------------------------

        teacher.EmployeeNumber =
            request.EmployeeNumber;

        teacher.PhoneNumber =
            request.PhoneNumber;

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                teacherId = teacher.Id,
                userId = teacher.UserId,
                fullName = teacher.User.FullName,
                email = teacher.User.Email,
                phoneNumber = teacher.User.PhoneNumber,
                employeeNumber = teacher.EmployeeNumber,
                schoolId = teacher.SchoolId
            },
            null
        );
    }

    // ================================================================
    // DELETE TEACHER
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        DeleteTeacherAsync(
            Guid schoolId,
            Guid teacherId)
    {
        var teacher = await _context.Teachers
            .FirstOrDefaultAsync(x =>
                x.Id == teacherId &&
                x.SchoolId == schoolId);

        if (teacher == null)
        {
            return (
                false,
                null,
                "Teacher not found."
            );
        }

        var user = await _userManager
            .FindByIdAsync(teacher.UserId.ToString());

        if (user == null)
        {
            return (
                false,
                null,
                "Teacher user account not found."
            );
        }

        // Delete Teacher profile first
        _context.Teachers.Remove(teacher);

        await _context.SaveChangesAsync();

        // Delete Identity user
        var result = await _userManager
            .DeleteAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                ", ",
                result.Errors.Select(x => x.Description));

            return (
                false,
                null,
                errors
            );
        }

        return (
            true,
            new
            {
                teacherId,
                message = "Teacher deleted successfully."
            },
            null
        );
    }
}