using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.Teachers;
using UserManagementApi.Models;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

public class TeacherService : ITeacherService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserManagementService _userManagementService;

    public TeacherService(
        ApplicationDbContext context,
        IUserManagementService userManagementService)
    {
        _context = context;
        _userManagementService = userManagementService;
    }

    public async Task<(bool Success, object? Data, string? Error)>
        CreateTeacherAsync(
            Guid schoolId,
            CreateTeacherRequest request)
    {
        // 1. Check school
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

        // 2. Create Identity user + Teacher role
        var userResult =
            await _userManagementService.CreateUserAsync(
                request.FullName,
                request.Email,
                request.Password,
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

        // 3. Create Teacher profile
        var teacherProfile = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SchoolId = schoolId,
            EmployeeNumber = request.EmployeeNumber
        };

        _context.Teachers.Add(teacherProfile);

        // 4. Save
        await _context.SaveChangesAsync();

        // 5. Return teacher information
        return (
            true,
            new
            {
                user.Id,
                user.FullName,
                user.Email,
                teacherProfile.EmployeeNumber,
                SchoolId = school.Id,
                SchoolName = school.Name
            },
            null
        );
    }

    public async Task<IEnumerable<object>> GetTeachersAsync(
        Guid schoolId)
    {
        return await _context.Teachers
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.EmployeeNumber,
                x.User.FullName,
                x.User.Email
            })
            .ToListAsync();
    }
}