using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

public class StudentPortalService : IStudentPortalService
{
    private readonly ApplicationDbContext _context;

    public StudentPortalService(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, object? Data, string? Error)>
        GetProfileAsync(string userId)
    {
        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new
            {
                StudentId = x.Id,
                x.UserId,

                FullName = x.User.FullName,
                Email = x.User.Email,
                PhoneNumber = x.User.PhoneNumber,

                x.StudentNumber,

                SchoolId = x.SchoolId,
                SchoolName = x.School.Name,

                ClassId = x.ClassId,
                ClassName = x.Class.Name
            })
            .FirstOrDefaultAsync();

        if (student == null)
        {
            return (
                false,
                null,
                "Student profile not found."
            );
        }

        return (
            true,
            student,
            null
        );
    }

    public async Task<(bool Success, object? Data, string? Error)>
        GetClassAsync(string userId)
    {
        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new
            {
                StudentId = x.Id,
                ClassId = x.ClassId,
                ClassName = x.Class.Name,
                SchoolId = x.SchoolId,
                SchoolName = x.School.Name
            })
            .FirstOrDefaultAsync();

        if (student == null)
        {
            return (
                false,
                null,
                "Student profile not found."
            );
        }

        if (student.ClassId == null)
        {
            return (
                false,
                null,
                "Student is not assigned to a class."
            );
        }

        return (
            true,
            student,
            null
        );
    }

    public async Task<(bool Success, object? Data, string? Error)>
        GetSubjectsAsync(string userId)
    {
        var student = await _context.StudentProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.UserId == userId);

        if (student == null)
        {
            return (
                false,
                null,
                "Student profile not found."
            );
        }

        if (student.ClassId == null)
        {
            return (
                false,
                null,
                "Student is not assigned to a class."
            );
        }

        // Subjects belonging to the student's school.
        var subjects = await _context.Subjects
            .AsNoTracking()
            .Where(x =>
                x.SchoolId == student.SchoolId)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.SchoolId
            })
            .ToListAsync();

        return (
            true,
            subjects,
            null
        );
    }

    public async Task<(bool Success, object? Data, string? Error)>
    GetResultsAsync(
        string userId,
        string session,
        string term)
    {
        var student = await _context.StudentProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.UserId == userId);

        if (student == null)
        {
            return (
                false,
                null,
                "Student profile not found."
            );
        }

        var results = await _context.StudentResults
            .AsNoTracking()
            .Where(x =>
                x.StudentId == student.Id &&
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

        return (
            true,
            new
            {
                StudentId = student.Id,
                StudentNumber = student.StudentNumber,
                Session = session,
                Term = term,
                Results = results,
                TotalSubjects = results.Count,
                TotalScore = results.Sum(x => x.Score),
                AverageScore = results.Count == 0
                    ? 0
                    : results.Average(x => x.Score)
            },
            null
        );
    }
}