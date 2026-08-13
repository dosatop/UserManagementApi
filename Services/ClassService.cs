using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Classes;
using UserManagementApi.Models;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

public class ClassService : IClassService
{
    private readonly ApplicationDbContext _context;

    public ClassService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, object? Data, string? Error)>
        CreateClassAsync(
            Guid schoolId,
            CreateClassRequest request)
    {
        var school = await _context.Schools
            .FirstOrDefaultAsync(x => x.Id == schoolId);

        if (school == null)
        {
            return (false, null, "School not found.");
        }

        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, null, "Class name is required.");
        }

        var exists = await _context.Classes
            .AnyAsync(x =>
                x.SchoolId == schoolId &&
                x.Name.ToLower() == name.ToLower());

        if (exists)
        {
            return (
                false,
                null,
                "A class with this name already exists."
            );
        }

        var classroom = new Class
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Name = name
        };

        _context.Classes.Add(classroom);

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                classroom.Id,
                classroom.Name,
                classroom.SchoolId,
                SchoolName = school.Name
            },
            null
        );
    }

    public async Task<IEnumerable<object>>
        GetClassesAsync(Guid schoolId)
    {
        return await _context.Classes
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.SchoolId
            })
            .ToListAsync();
    }

    public async Task<(bool Success, object? Data, string? Error)>
        GetClassAsync(
            Guid schoolId,
            Guid classId)
    {
        var classroom = await _context.Classes
            .AsNoTracking()
            .Where(x =>
                x.Id == classId &&
                x.SchoolId == schoolId)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.SchoolId
            })
            .FirstOrDefaultAsync();

        if (classroom == null)
        {
            return (false, null, "Class not found.");
        }

        return (true, classroom, null);
    }

    public async Task<(bool Success, object? Data, string? Error)>
        UpdateClassAsync(
            Guid schoolId,
            Guid classId,
            CreateClassRequest request)
    {
        var classroom = await _context.Classes
            .FirstOrDefaultAsync(x =>
                x.Id == classId &&
                x.SchoolId == schoolId);

        if (classroom == null)
        {
            return (false, null, "Class not found.");
        }

        var name = request.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, null, "Class name is required.");
        }

        var exists = await _context.Classes
            .AnyAsync(x =>
                x.SchoolId == schoolId &&
                x.Id != classId &&
                x.Name.ToLower() == name.ToLower());

        if (exists)
        {
            return (
                false,
                null,
                "A class with this name already exists."
            );
        }

        classroom.Name = name;

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                classroom.Id,
                classroom.Name,
                classroom.SchoolId
            },
            null
        );
    }

    public async Task<(bool Success, string? Error)>
        DeleteClassAsync(
            Guid schoolId,
            Guid classId)
    {
        var classroom = await _context.Classes
            .FirstOrDefaultAsync(x =>
                x.Id == classId &&
                x.SchoolId == schoolId);

        if (classroom == null)
        {
            return (false, "Class not found.");
        }

        var hasStudents = await _context.StudentProfiles
            .AnyAsync(x => x.ClassId == classId);

        if (hasStudents)
        {
            return (
                false,
                "This class cannot be deleted because it has students."
            );
        }

        _context.Classes.Remove(classroom);

        await _context.SaveChangesAsync();

        return (true, null);
    }
}