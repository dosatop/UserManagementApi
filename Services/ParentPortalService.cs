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

    // ============================================================
    // PRIVATE HELPERS
    // ============================================================

    /// <summary>
    /// Gets the parent profile for the authenticated user.
    /// </summary>
    private async Task<Models.Parent?> GetParentAsync(
        string userId)
    {
        return await _context.Parents
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.UserId == userId);
    }

    /// <summary>
    /// Checks whether the specified student belongs
    /// to the authenticated parent.
    /// </summary>
    private async Task<bool> IsChildOfParentAsync(
        Guid parentId,
        Guid studentId)
    {
        return await _context.ParentStudents
            .AsNoTracking()
            .AnyAsync(x =>
                x.ParentId == parentId &&
                x.StudentId == studentId);
    }

    // ============================================================
    // PROFILE
    // ============================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetProfileAsync(
            string userId)
    {
        var parent = await _context.Parents
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new
            {
                x.ParentId,
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

        return (
            true,
            parent,
            null
        );
    }

    // ============================================================
    // CHILDREN
    // ============================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetChildrenAsync(
            string userId)
    {
        var parent = await GetParentAsync(userId);

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
            .Where(x =>
                x.ParentId == parent.ParentId)
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
            .ToListAsync();

        return (
            true,
            children,
            null
        );
    }

    // ============================================================
    // SINGLE CHILD
    // ============================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetChildAsync(
            string userId,
            Guid studentId)
    {
        var parent = await GetParentAsync(userId);

        if (parent == null)
        {
            return (
                false,
                null,
                "Parent profile not found."
            );
        }

        // IMPORTANT:
        // Only allow the parent to access students
        // linked through ParentStudents.

        var child = await _context.ParentStudents
            .AsNoTracking()
            .Where(x =>
                x.ParentId == parent.ParentId &&
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

    // ============================================================
    // CHILD CLASS
    // ============================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetChildClassAsync(
            string userId,
            Guid studentId)
    {
        var parent = await GetParentAsync(userId);

        if (parent == null)
        {
            return (
                false,
                null,
                "Parent profile not found."
            );
        }

        var linked = await IsChildOfParentAsync(
            parent.ParentId,
            studentId);

        if (!linked)
        {
            return (
                false,
                null,
                "Student not found or is not linked to this parent."
            );
        }

        var child = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x =>
                x.Id == studentId)
            .Select(x => new
            {
                StudentId = x.Id,

                x.StudentNumber,

                ClassId = x.ClassId,
                ClassName = x.Class.Name,

                SchoolId = x.SchoolId,
                SchoolName = x.School.Name
            })
            .FirstOrDefaultAsync();

        if (child == null)
        {
            return (
                false,
                null,
                "Student not found."
            );
        }

        return (
            true,
            child,
            null
        );
    }

    // ============================================================
    // CHILD SUBJECTS
    // ============================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetChildSubjectsAsync(
            string userId,
            Guid studentId)
    {
        var parent = await GetParentAsync(userId);

        if (parent == null)
        {
            return (
                false,
                null,
                "Parent profile not found."
            );
        }

        var linked = await IsChildOfParentAsync(
            parent.ParentId,
            studentId);

        if (!linked)
        {
            return (
                false,
                null,
                "Student not found or is not linked to this parent."
            );
        }

        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x =>
                x.Id == studentId)
            .Select(x => new
            {
                x.Id,
                x.StudentNumber,
                x.SchoolId,
                x.ClassId
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
            new
            {
                StudentId = student.Id,

                StudentNumber = student.StudentNumber,

                TotalSubjects = subjects.Count,

                Subjects = subjects
            },
            null
        );
    }

    // ============================================================
    // CHILD RESULTS
    // ============================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetChildResultsAsync(
            string userId,
            Guid studentId,
            string session,
            string term)
    {
        var parent = await GetParentAsync(userId);

        if (parent == null)
        {
            return (
                false,
                null,
                "Parent profile not found."
            );
        }

        // IMPORTANT:
        // Verify that this child belongs to this parent.

        var linked = await IsChildOfParentAsync(
            parent.ParentId,
            studentId);

        if (!linked)
        {
            return (
                false,
                null,
                "You do not have access to this student's results."
            );
        }

        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x =>
                x.Id == studentId)
            .Select(x => new
            {
                x.Id,
                x.StudentNumber,

                FullName = x.User.FullName,

                ClassId = x.ClassId,
                ClassName = x.Class.Name,

                SchoolId = x.SchoolId
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

        var results = await _context.StudentResults
            .AsNoTracking()
            .Where(x =>
                x.StudentId == studentId &&
                x.SchoolId == student.SchoolId &&
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

        var totalScore = results.Sum(x =>
            x.Score);

        var averageScore = results.Count == 0
            ? 0
            : results.Average(x =>
                x.Score);

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

    // ============================================================
    // CHILD ATTENDANCE
    // ============================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetChildAttendanceAsync(
            string userId,
            Guid studentId,
            string session,
            string term)
    {
        var parent = await GetParentAsync(userId);

        if (parent == null)
        {
            return (
                false,
                null,
                "Parent profile not found."
            );
        }

        var linked = await IsChildOfParentAsync(
            parent.ParentId,
            studentId);

        if (!linked)
        {
            return (
                false,
                null,
                "You do not have access to this student's attendance."
            );
        }

        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x =>
                x.Id == studentId)
            .Select(x => new
            {
                x.Id,
                x.StudentNumber,

                x.SchoolId,
                x.ClassId,

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

        var attendance = await _context.AttendanceRecords
            .AsNoTracking()
            .Where(x =>
                x.StudentId == studentId &&
                x.SchoolId == student.SchoolId &&
                x.ClassId == student.ClassId &&
                x.Session == session &&
                x.Term == term)
            .OrderByDescending(x =>
                x.AttendanceDate)
            .Select(x => new
            {
                x.Id,

                x.AttendanceDate,

                Status = x.Status.ToString(),

                x.Remarks,

                SubjectId = x.SubjectId,

                SubjectName = x.Subject != null
                    ? x.Subject.Name
                    : null,

                TeacherId = x.TeacherId,

                ClassId = x.ClassId,

                Session = x.Session,
                Term = x.Term
            })
            .ToListAsync();

        var total = attendance.Count;

        var present = attendance.Count(x =>
            x.Status == "Present");

        var absent = attendance.Count(x =>
            x.Status == "Absent");

        var late = attendance.Count(x =>
            x.Status == "Late");

        var excused = attendance.Count(x =>
            x.Status == "Excused");

        var holiday = attendance.Count(x =>
            x.Status == "Holiday");

        return (
            true,
            new
            {
                StudentId = student.Id,

                StudentNumber = student.StudentNumber,

                ClassId = student.ClassId,

                ClassName = student.ClassName,

                Session = session,
                Term = term,

                Summary = new
                {
                    TotalRecords = total,

                    Present = present,

                    Absent = absent,

                    Late = late,

                    Excused = excused,

                    Holiday = holiday,

                    AttendancePercentage = total == 0
                        ? 0
                        : Math.Round(
                            (decimal)present /
                            total *
                            100,
                            2)
                },

                Records = attendance
            },
            null
        );
    }

    // ============================================================
    // CHILD ASSIGNMENTS
    // ============================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetChildAssignmentsAsync(
            string userId,
            Guid studentId,
            string session,
            string term)
    {
        var parent = await GetParentAsync(userId);

        if (parent == null)
        {
            return (
                false,
                null,
                "Parent profile not found."
            );
        }

        var linked = await IsChildOfParentAsync(
            parent.ParentId,
            studentId);

        if (!linked)
        {
            return (
                false,
                null,
                "You do not have access to this student's assignments."
            );
        }

        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x =>
                x.Id == studentId)
            .Select(x => new
            {
                x.Id,
                x.StudentNumber,

                x.SchoolId,
                x.ClassId,

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

        var assignments = await _context.Assignments
            .AsNoTracking()
            .Where(x =>
                x.ClassId == student.ClassId &&
                x.SchoolId == student.SchoolId &&
                x.Session == session &&
                x.Term == term &&
                x.IsPublished)
            .OrderByDescending(x =>
                x.AssignedAt)
            .Select(x => new
            {
                x.Id,

                x.Title,
                x.Description,
                x.AttachmentUrl,

                x.AssignedAt,
                x.DueDate,

                x.Session,
                x.Term,

                ClassId = x.ClassId,

                SubjectId = x.SubjectId,
                SubjectName = x.Subject.Name,

                TeacherId = x.TeacherId,
                TeacherName = x.Teacher.User.FullName,

                Submission = x.Submissions
                    .Where(s =>
                        s.StudentId == student.Id)
                    .Select(s => new
                    {
                        s.Id,

                        s.SubmissionText,
                        s.AttachmentUrl,
                        s.SubmittedAt,

                        s.Score,
                        s.Feedback,

                        s.IsGraded,
                        s.GradedAt
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        return (
            true,
            new
            {
                StudentId = student.Id,

                StudentNumber = student.StudentNumber,

                ClassId = student.ClassId,

                ClassName = student.ClassName,

                Session = session,
                Term = term,

                TotalAssignments =
                    assignments.Count,

                Assignments = assignments
            },
            null
        );
    }

    // ============================================================
    // SINGLE CHILD ASSIGNMENT
    // ============================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetChildAssignmentAsync(
            string userId,
            Guid studentId,
            Guid assignmentId)
    {
        var parent = await GetParentAsync(userId);

        if (parent == null)
        {
            return (
                false,
                null,
                "Parent profile not found."
            );
        }

        var linked = await IsChildOfParentAsync(
            parent.ParentId,
            studentId);

        if (!linked)
        {
            return (
                false,
                null,
                "You do not have access to this student's assignment."
            );
        }

        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x =>
                x.Id == studentId)
            .Select(x => new
            {
                x.Id,

                x.StudentNumber,

                x.SchoolId,
                x.ClassId
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

        var assignment = await _context.Assignments
            .AsNoTracking()
            .Where(x =>
                x.Id == assignmentId &&
                x.ClassId == student.ClassId &&
                x.SchoolId == student.SchoolId &&
                x.IsPublished)
            .Select(x => new
            {
                x.Id,

                x.Title,
                x.Description,
                x.AttachmentUrl,

                x.AssignedAt,
                x.DueDate,

                x.Session,
                x.Term,

                ClassId = x.ClassId,

                SubjectId = x.SubjectId,
                SubjectName = x.Subject.Name,

                TeacherId = x.TeacherId,
                TeacherName = x.Teacher.User.FullName,

                Submission = x.Submissions
                    .Where(s =>
                        s.StudentId == student.Id)
                    .Select(s => new
                    {
                        s.Id,

                        s.SubmissionText,
                        s.AttachmentUrl,
                        s.SubmittedAt,

                        s.Score,
                        s.Feedback,

                        s.IsGraded,
                        s.GradedAt
                    })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (assignment == null)
        {
            return (
                false,
                null,
                "Assignment not found."
            );
        }

        return (
            true,
            assignment,
            null
        );
    }
}