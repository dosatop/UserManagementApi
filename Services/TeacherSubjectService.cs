using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Teachers;
using UserManagementApi.Models;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

public class TeacherSubjectService : ITeacherSubjectService
{
    private readonly ApplicationDbContext _context;

    public TeacherSubjectService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, object? Data, string? Error)>
        AssignSubjectAsync(
            Guid schoolId,
            Guid teacherId,
            AssignTeacherSubjectRequest request)
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
                "Teacher not found in this school."
            );
        }

        var subject = await _context.Subjects
            .FirstOrDefaultAsync(x =>
                x.Id == request.SubjectId &&
                x.SchoolId == schoolId);

        if (subject == null)
        {
            return (
                false,
                null,
                "Subject not found in this school."
            );
        }

        var exists = await _context.TeacherSubjects
            .AnyAsync(x =>
                x.TeacherId == teacherId &&
                x.SubjectId == request.SubjectId);

        if (exists)
        {
            return (
                false,
                null,
                "This subject is already assigned to this teacher."
            );
        }

        var teacherSubject = new TeacherSubject
        {
            TeacherId = teacherId,
            SubjectId = request.SubjectId
        };

        _context.TeacherSubjects.Add(teacherSubject);

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                TeacherId = teacher.Id,
                TeacherName = teacher.User.FullName,
                SubjectId = subject.Id,
                SubjectName = subject.Name,
                SchoolId = schoolId
            },
            null
        );
    }

    public async Task<IEnumerable<object>> GetTeacherSubjectsAsync(
        Guid schoolId,
        Guid teacherId)
    {
        return await _context.TeacherSubjects
            .AsNoTracking()
            .Where(x =>
                x.TeacherId == teacherId &&
                x.Teacher.SchoolId == schoolId)
            .Select(x => new
            {
                x.SubjectId,
                SubjectName = x.Subject.Name
            })
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error)>
        RemoveSubjectAsync(
            Guid schoolId,
            Guid teacherId,
            Guid subjectId)
    {
        var teacherSubject =
            await _context.TeacherSubjects
                .FirstOrDefaultAsync(x =>
                    x.TeacherId == teacherId &&
                    x.SubjectId == subjectId &&
                    x.Teacher.SchoolId == schoolId);

        if (teacherSubject == null)
        {
            return (
                false,
                "Teacher is not assigned to this subject."
            );
        }

        _context.TeacherSubjects.Remove(teacherSubject);

        await _context.SaveChangesAsync();

        return (true, null);
    }
}