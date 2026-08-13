using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Subjects;
using UserManagementApi.Models;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

public class SubjectService : ISubjectService
{
    private readonly ApplicationDbContext _context;

    public SubjectService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, object? Data, string? Error)>
        CreateSubjectAsync(
            Guid schoolId,
            CreateSubjectRequest request)
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
                "Subject name is required."
            );
        }

        var exists = await _context.Subjects
            .AnyAsync(x =>
                x.SchoolId == schoolId &&
                x.Name.ToLower() == name.ToLower());

        if (exists)
        {
            return (
                false,
                null,
                "A subject with this name already exists."
            );
        }

        if (!string.IsNullOrWhiteSpace(code))
        {
            var codeExists = await _context.Subjects
                .AnyAsync(x =>
                    x.SchoolId == schoolId &&
                    x.Code != null &&
                    x.Code.ToLower() == code.ToLower());

            if (codeExists)
            {
                return (
                    false,
                    null,
                    "A subject with this code already exists."
                );
            }
        }

        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Name = name,
            Code = code
        };

        _context.Subjects.Add(subject);

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                subject.Id,
                subject.Name,
                subject.Code,
                subject.SchoolId,
                SchoolName = school.Name
            },
            null
        );
    }

    public async Task<IEnumerable<object>>
        GetSubjectsAsync(Guid schoolId)
    {
        return await _context.Subjects
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

    public async Task<(bool Success, object? Data, string? Error)>
        GetSubjectAsync(
            Guid schoolId,
            Guid subjectId)
    {
        var subject = await _context.Subjects
            .AsNoTracking()
            .Where(x =>
                x.Id == subjectId &&
                x.SchoolId == schoolId)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Code,
                x.SchoolId
            })
            .FirstOrDefaultAsync();

        if (subject == null)
        {
            return (
                false,
                null,
                "Subject not found."
            );
        }

        return (
            true,
            subject,
            null
        );
    }

    public async Task<(bool Success, object? Data, string? Error)>
        UpdateSubjectAsync(
            Guid schoolId,
            Guid subjectId,
            CreateSubjectRequest request)
    {
        var subject = await _context.Subjects
            .FirstOrDefaultAsync(x =>
                x.Id == subjectId &&
                x.SchoolId == schoolId);

        if (subject == null)
        {
            return (
                false,
                null,
                "Subject not found."
            );
        }

        var name = request.Name.Trim();
        var code = request.Code?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return (
                false,
                null,
                "Subject name is required."
            );
        }

        var nameExists = await _context.Subjects
            .AnyAsync(x =>
                x.SchoolId == schoolId &&
                x.Id != subjectId &&
                x.Name.ToLower() == name.ToLower());

        if (nameExists)
        {
            return (
                false,
                null,
                "A subject with this name already exists."
            );
        }

        if (!string.IsNullOrWhiteSpace(code))
        {
            var codeExists = await _context.Subjects
                .AnyAsync(x =>
                    x.SchoolId == schoolId &&
                    x.Id != subjectId &&
                    x.Code != null &&
                    x.Code.ToLower() == code.ToLower());

            if (codeExists)
            {
                return (
                    false,
                    null,
                    "A subject with this code already exists."
                );
            }
        }

        subject.Name = name;
        subject.Code = code;

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                subject.Id,
                subject.Name,
                subject.Code,
                subject.SchoolId
            },
            null
        );
    }

    public async Task<(bool Success, string? Error)>
        DeleteSubjectAsync(
            Guid schoolId,
            Guid subjectId)
    {
        var subject = await _context.Subjects
            .FirstOrDefaultAsync(x =>
                x.Id == subjectId &&
                x.SchoolId == schoolId);

        if (subject == null)
        {
            return (
                false,
                "Subject not found."
            );
        }

        var hasTeacherAssignments =
            await _context.TeacherSubjects
                .AnyAsync(x => x.SubjectId == subjectId);

        if (hasTeacherAssignments)
        {
            return (
                false,
                "This subject cannot be deleted because teachers are assigned to it."
            );
        }

        _context.Subjects.Remove(subject);

        await _context.SaveChangesAsync();

        return (true, null);
    }
}