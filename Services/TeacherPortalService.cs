using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

public class TeacherPortalService : ITeacherPortalService
{
    private readonly ApplicationDbContext _context;

    public TeacherPortalService(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, object? Data, string? Error)>
        GetProfileAsync(string userId)
    {
        var teacher = await _context.Teachers
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.EmployeeNumber,
                SchoolId = x.SchoolId,
                SchoolName = x.School.Name,
                FullName = x.User.FullName,
                Email = x.User.Email
            })
            .FirstOrDefaultAsync();

        if (teacher == null)
        {
            return (
                false,
                null,
                "Teacher profile not found."
            );
        }

        return (
            true,
            teacher,
            null
        );
    }

    public async Task<(bool Success, object? Data, string? Error)>
        GetClassesAsync(string userId)
    {
        var teacher = await _context.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.UserId == userId);

        if (teacher == null)
        {
            return (
                false,
                null,
                "Teacher profile not found."
            );
        }

        var classes = await _context.TeacherClasses
    .AsNoTracking()
    .Where(x =>
        x.TeacherId == teacher.Id &&
        x.Class.SchoolId == teacher.SchoolId)
    .Select(x => new
    {
        x.ClassId,
        ClassName = x.Class.Name,
        SchoolId = x.Class.SchoolId
    })
    .ToListAsync();

        return (
            true,
            classes,
            null
        );
    }

    public async Task<(bool Success, object? Data, string? Error)>
        GetSubjectsAsync(string userId)
    {
        var teacher = await _context.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.UserId == userId);

        if (teacher == null)
        {
            return (
                false,
                null,
                "Teacher profile not found."
            );
        }

        var subjects = await _context.TeacherSubjects
            .AsNoTracking()
            .Where(x => x.TeacherId == teacher.Id)
            .Select(x => new
            {
                x.SubjectId,
                SubjectName = x.Subject.Name,
                x.Subject.SchoolId
            })
            .ToListAsync();

        return (
            true,
            subjects,
            null
        );
    }
}