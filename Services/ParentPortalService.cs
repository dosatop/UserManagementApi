using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

public class ParentPortalService : IParentPortalService
{
    private readonly ApplicationDbContext _context;

    public ParentPortalService(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, object? Data, string? Error)>
        GetProfileAsync(string userId)
    {
        var parent = await _context.Parents
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                FullName = x.User.FullName,
                Email = x.User.Email,
                PhoneNumber = x.User.PhoneNumber,
                SchoolId = x.SchoolId,
                SchoolName = x.School.Name
            })
            .FirstOrDefaultAsync();

        if (parent == null)
        {
            return (
                false,
                null,
                "Parent profile not found."
            );
        }

        return (true, parent, null);
    }

    public async Task<(bool Success, object? Data, string? Error)>
        GetChildrenAsync(string userId)
    {
        var parent = await _context.Parents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (parent == null)
        {
            return (
                false,
                null,
                "Parent profile not found."
            );
        }

        var children = await _context.ParentStudents
            .AsNoTracking()
            .Where(x => x.ParentId == parent.Id)
            .Select(x => new
            {
                StudentId = x.Student.Id,
                x.Student.StudentNumber,

                FullName = x.Student.User.FullName,
                Email = x.Student.User.Email,

                ClassId = x.Student.ClassId,
                ClassName = x.Student.Class.Name
            })
            .ToListAsync();

        return (
            true,
            children,
            null
        );
    }

    public async Task<(bool Success, object? Data, string? Error)>
        GetChildAsync(
            string userId,
            Guid studentId)
    {
        var parent = await _context.Parents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (parent == null)
        {
            return (
                false,
                null,
                "Parent profile not found."
            );
        }

        // Important:
        // We check ParentStudents first so a parent
        // cannot access another student's information.
        var child = await _context.ParentStudents
            .AsNoTracking()
            .Where(x =>
                x.ParentId == parent.Id &&
                x.StudentId == studentId)
            .Select(x => new
            {
                StudentId = x.Student.Id,
                x.Student.StudentNumber,

                FullName = x.Student.User.FullName,
                Email = x.Student.User.Email,

                ClassId = x.Student.ClassId,
                ClassName = x.Student.Class.Name,

                SchoolId = x.Student.SchoolId,
                SchoolName = x.Student.School.Name
            })
            .FirstOrDefaultAsync();

        if (child == null)
        {
            return (
                false,
                null,
                "Student not found or is not linked to this parent."
            );
        }

        return (
            true,
            child,
            null
        );
    }

    public async Task<(bool Success, object? Data, string? Error)>
    GetChildResultsAsync(
        string userId,
        Guid studentId,
        string session,
        string term)
    {
        // Find parent
        var parent = await _context.Parents
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.UserId == userId);

        if (parent == null)
        {
            return (
                false,
                null,
                "Parent profile not found."
            );
        }

        // IMPORTANT:
        // Make sure this student actually belongs
        // to this parent.
        var linked = await _context.ParentStudents
            .AsNoTracking()
            .AnyAsync(x =>
                x.ParentId == parent.Id &&
                x.StudentId == studentId);

        if (!linked)
        {
            return (
                false,
                null,
                "You do not have access to this student's results."
            );
        }

        // Get student information
        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x => x.Id == studentId)
            .Select(x => new
            {
                x.Id,
                x.StudentNumber,
                FullName = x.User.FullName,
                ClassName = x.Class.Name
            })
            .FirstOrDefaultAsync();

        if (student == null)
        {
            return (
                false,
                null,
                "Student not found."
            );
        }

        // Get results
        var results = await _context.StudentResults
            .AsNoTracking()
            .Where(x =>
                x.StudentId == studentId &&
                x.Session == session &&
                x.Term == term)
            .Select(x => new
            {
                x.Id,

                SubjectId = x.SubjectId,
                SubjectName = x.Subject.Name,

                x.TestScore,
                x.ExamScore,
                x.Score,
                x.Grade,
                x.Remark
            })
            .ToListAsync();

        var totalScore = results.Sum(x => x.Score);

        var averageScore = results.Count == 0
            ? 0
            : results.Average(x => x.Score);

        return (
            true,
            new
            {
                Student = student,

                Session = session,
                Term = term,

                TotalSubjects = results.Count,

                TotalScore = totalScore,

                AverageScore = averageScore,

                Results = results
            },
            null
        );
    }
}