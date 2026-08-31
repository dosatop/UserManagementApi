using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Departments;
using UserManagementApi.Models;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

public class DepartmentService : IDepartmentService
{
    private readonly ApplicationDbContext _context;

    public DepartmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // CREATE
    // ============================================================

    public async Task<(bool Success, object? Data, string? Error)>
        CreateDepartmentAsync(
            Guid schoolId,
            CreateDepartmentRequest request)
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

        var name = request.Name.Trim();
        var code = request.Code?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return (
                false,
                null,
                "Department name is required."
            );
        }

        var exists = await _context.Departments
            .AnyAsync(x =>
                x.SchoolId == schoolId &&
                x.Name.ToLower() == name.ToLower());

        if (exists)
        {
            return (
                false,
                null,
                "A department with this name already exists."
            );
        }

        if (!string.IsNullOrWhiteSpace(code))
        {
            var codeExists = await _context.Departments
                .AnyAsync(x =>
                    x.SchoolId == schoolId &&
                    x.Code != null &&
                    x.Code.ToLower() == code.ToLower());

            if (codeExists)
            {
                return (
                    false,
                    null,
                    "A department with this code already exists."
                );
            }
        }

        var department = new Department
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Name = name,
            Code = code
        };

        _context.Departments.Add(department);

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                department.Id,
                department.Name,
                department.Code,
                department.SchoolId
            },
            null
        );
    }

    // ============================================================
    // GET ALL
    // ============================================================

    public async Task<IEnumerable<object>>
        GetDepartmentsAsync(Guid schoolId)
    {
        return await _context.Departments
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Code,
                x.SchoolId
            })
            .ToListAsync();
    }

    // ============================================================
    // GET ONE
    // ============================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetDepartmentAsync(
            Guid schoolId,
            Guid departmentId)
    {
        var department = await _context.Departments
            .AsNoTracking()
            .Where(x =>
                x.Id == departmentId &&
                x.SchoolId == schoolId)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Code,
                x.SchoolId,

                subjectCount = x.Subjects.Count
            })
            .FirstOrDefaultAsync();

        if (department == null)
        {
            return (
                false,
                null,
                "Department not found."
            );
        }

        return (
            true,
            department,
            null
        );
    }

    // ============================================================
    // UPDATE
    // ============================================================

    public async Task<(bool Success, object? Data, string? Error)>
        UpdateDepartmentAsync(
            Guid schoolId,
            Guid departmentId,
            CreateDepartmentRequest request)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(x =>
                x.Id == departmentId &&
                x.SchoolId == schoolId);

        if (department == null)
        {
            return (
                false,
                null,
                "Department not found."
            );
        }

        var name = request.Name.Trim();
        var code = request.Code?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return (
                false,
                null,
                "Department name is required."
            );
        }

        var nameExists = await _context.Departments
            .AnyAsync(x =>
                x.SchoolId == schoolId &&
                x.Id != departmentId &&
                x.Name.ToLower() == name.ToLower());

        if (nameExists)
        {
            return (
                false,
                null,
                "A department with this name already exists."
            );
        }

        if (!string.IsNullOrWhiteSpace(code))
        {
            var codeExists = await _context.Departments
                .AnyAsync(x =>
                    x.SchoolId == schoolId &&
                    x.Id != departmentId &&
                    x.Code != null &&
                    x.Code.ToLower() == code.ToLower());

            if (codeExists)
            {
                return (
                    false,
                    null,
                    "A department with this code already exists."
                );
            }
        }

        department.Name = name;
        department.Code = code;

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                department.Id,
                department.Name,
                department.Code,
                department.SchoolId
            },
            null
        );
    }

    // ============================================================
    // DELETE
    // ============================================================

    public async Task<(bool Success, string? Error)>
        DeleteDepartmentAsync(
            Guid schoolId,
            Guid departmentId)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(x =>
                x.Id == departmentId &&
                x.SchoolId == schoolId);

        if (department == null)
        {
            return (
                false,
                "Department not found."
            );
        }

        var hasSubjects = await _context.Subjects
            .AnyAsync(x =>
                x.DepartmentId == departmentId);

        if (hasSubjects)
        {
            return (
                false,
                "This department cannot be deleted because subjects are assigned to it."
            );
        }

        _context.Departments.Remove(department);

        await _context.SaveChangesAsync();

        return (true, null);
    }
}
