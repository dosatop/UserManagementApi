using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

// ====================================================================
// PARENT PORTAL SERVICE
// ====================================================================
// Changes from the previous version:
//   - session / term are OPTIONAL for results, attendance and
//     assignments. When missing, the school's current session and
//     current term are used (the app doesn't know them).
//   - Subjects come from the child's CLASS (ClassSubjects), not every
//     subject in the school, and include code + teacher name.
//   - Attendance % counts Late as attended and ignores Excused and
//     Holiday days (same rule as the teacher and student apps).
//   - Responses include the session/term actually used.
// ====================================================================

public class ParentPortalService : IParentPortalService
{
    private readonly ApplicationDbContext _context;

    public ParentPortalService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // PRIVATE HELPERS
    // ============================================================

    private async Task<Models.Parent?> GetParentAsync(string userId)
    {
        return await _context.Parents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    private async Task<bool> IsChildOfParentAsync(Guid parentId, Guid studentId)
    {
        return await _context.ParentStudents
            .AsNoTracking()
            .AnyAsync(x => x.ParentId == parentId && x.StudentId == studentId);
    }

    /// <summary>
    /// Checks the parent exists and the student is linked to them.
    /// Returns an error message, or null when access is allowed.
    /// </summary>
    private async Task<string?> CheckAccessAsync(
        string userId,
        Guid studentId,
        string deniedMessage)
    {
        var parent = await GetParentAsync(userId);

        if (parent == null)
        {
            return "Parent profile not found.";
        }

        var linked = await IsChildOfParentAsync(parent.ParentId, studentId);

        return linked ? null : deniedMessage;
    }

    /// <summary>
    /// Uses the given session/term, or falls back to the school's
    /// current session and current term.
    /// </summary>
    private async Task<(string? Session, string? Term, string? Error)>
        ResolvePeriodAsync(Guid schoolId, string? session, string? term)
    {
        if (!string.IsNullOrWhiteSpace(session) &&
            !string.IsNullOrWhiteSpace(term))
        {
            return (session.Trim(), term.Trim(), null);
        }

        var current = await _context.AcademicSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.IsCurrent);

        var resolvedSession = string.IsNullOrWhiteSpace(session)
            ? current?.Session
            : session.Trim();

        if (string.IsNullOrWhiteSpace(resolvedSession))
        {
            return (null, null, "There is no active academic session.");
        }

        var resolvedTerm = term?.Trim();

        if (string.IsNullOrWhiteSpace(resolvedTerm) && current != null)
        {
            resolvedTerm = await _context.AcademicTerms
                .AsNoTracking()
                .Where(x => x.AcademicSessionId == current.Id && x.IsCurrent)
                .Select(x => x.Term)
                .FirstOrDefaultAsync();
        }

        if (string.IsNullOrWhiteSpace(resolvedTerm))
        {
            return (null, null, "There is no active academic term.");
        }

        return (resolvedSession, resolvedTerm, null);
    }

    // ============================================================
    // PROFILE
    // ============================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetProfileAsync(string userId)
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
                SchoolName = x.School.Name,
                ChildrenCount = _context.ParentStudents
                    .Count(ps => ps.ParentId == x.ParentId)
            })
            .FirstOrDefaultAsync();

        return parent == null
            ? (false, null, "Parent profile not found.")
            : (true, parent, null);
    }

    // ============================================================
    // CHILDREN
    // ============================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetChildrenAsync(string userId)
    {
        var parent = await GetParentAsync(userId);

        if (parent == null)
        {
            return (false, null, "Parent profile not found.");
        }

        var children = await _context.ParentStudents
            .AsNoTracking()
            .Where(x => x.ParentId == parent.ParentId)
            .OrderBy(x => x.Student.User.FullName)
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

        return (true, children, null);
    }

    // ============================================================
    // SINGLE CHILD
    // ============================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetChildAsync(string userId, Guid studentId)
    {
        var parent = await GetParentAsync(userId);

        if (parent == null)
        {
            return (false, null, "Parent profile not found.");
        }

        var child = await _context.ParentStudents
            .AsNoTracking()
            .Where(x => x.ParentId == parent.ParentId && x.StudentId == studentId)
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

        return child == null
            ? (false, null, "Student not found or is not linked to this parent.")
            : (true, child, null);
    }

    // ============================================================
    // CHILD CLASS
    // ============================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetChildClassAsync(string userId, Guid studentId)
    {
        var denied = await CheckAccessAsync(
            userId,
            studentId,
            "Student not found or is not linked to this parent.");

        if (denied != null)
        {
            return (false, null, denied);
        }

        var child = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x => x.Id == studentId)
            .Select(x => new
            {
                StudentId = x.Id,
                x.StudentNumber,
                ClassId = x.ClassId,
                ClassName = x.Class.Name,
                SchoolId = x.SchoolId,
                SchoolName = x.School.Name,

                ClassTeacherName = _context.ClassTeachers
                    .Where(ct => ct.ClassId == x.ClassId)
                    .Select(ct => ct.Teacher.User.FullName)
                    .FirstOrDefault(),

                StudentCount = _context.StudentProfiles
                    .Count(s => s.ClassId == x.ClassId)
            })
            .FirstOrDefaultAsync();

        return child == null
            ? (false, null, "Student not found.")
            : (true, child, null);
    }

    // ============================================================
    // CHILD SUBJECTS
    // ============================================================
    // Subjects offered in the child's class, with the teacher
    // assigned to each (class-specific first, then school-wide).

    public async Task<(bool Success, object? Data, string? Error)>
        GetChildSubjectsAsync(string userId, Guid studentId)
    {
        var denied = await CheckAccessAsync(
            userId,
            studentId,
            "Student not found or is not linked to this parent.");

        if (denied != null)
        {
            return (false, null, denied);
        }

        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x => x.Id == studentId)
            .Select(x => new { x.Id, x.StudentNumber, x.SchoolId, x.ClassId })
            .FirstOrDefaultAsync();

        if (student == null)
        {
            return (false, null, "Student not found.");
        }

        var subjects = await _context.ClassSubjects
            .AsNoTracking()
            .Where(cs => cs.ClassId == student.ClassId)
            .OrderBy(cs => cs.Subject.Name)
            .Select(cs => new
            {
                SubjectId = cs.SubjectId,
                SubjectName = cs.Subject.Name,
                Code = cs.Subject.Code,

                TeacherName = _context.TeacherSubjects
                    .Where(ts =>
                        ts.SubjectId == cs.SubjectId &&
                        (ts.ClassId == student.ClassId || ts.ClassId == null))
                    .OrderBy(ts => ts.ClassId == null ? 1 : 0)
                    .Select(ts => ts.Teacher.User.FullName)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return (
            true,
            new
            {
                StudentId = student.Id,
                student.StudentNumber,
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
            string? session,
            string? term)
    {
        var denied = await CheckAccessAsync(
            userId,
            studentId,
            "You do not have access to this student's results.");

        if (denied != null)
        {
            return (false, null, denied);
        }

        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x => x.Id == studentId)
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
            return (false, null, "Student not found.");
        }

        var period = await ResolvePeriodAsync(student.SchoolId, session, term);

        if (period.Error != null)
        {
            return (false, null, period.Error);
        }

        var results = await _context.StudentResults
            .AsNoTracking()
            .Where(x =>
                x.StudentId == studentId &&
                x.SchoolId == student.SchoolId &&
                x.Session == period.Session &&
                x.Term == period.Term)
            .OrderBy(x => x.Subject.Name)
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
                Session = period.Session,
                Term = period.Term,
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
            string? session,
            string? term)
    {
        var denied = await CheckAccessAsync(
            userId,
            studentId,
            "You do not have access to this student's attendance.");

        if (denied != null)
        {
            return (false, null, denied);
        }

        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x => x.Id == studentId)
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
            return (false, null, "Student not found.");
        }

        var period = await ResolvePeriodAsync(student.SchoolId, session, term);

        if (period.Error != null)
        {
            return (false, null, period.Error);
        }

        // Not filtered by the CURRENT class, so records stay visible
        // after a student is moved to another class mid-term.
        var attendance = await _context.AttendanceRecords
            .AsNoTracking()
            .Where(x =>
                x.StudentId == studentId &&
                x.SchoolId == student.SchoolId &&
                x.Session == period.Session &&
                x.Term == period.Term)
            .OrderByDescending(x => x.AttendanceDate)
            .Select(x => new
            {
                x.Id,
                x.AttendanceDate,
                Status = x.Status.ToString(),
                x.Remarks,
                SubjectId = x.SubjectId,
                SubjectName = x.Subject != null ? x.Subject.Name : null,
                TeacherId = x.TeacherId,
                ClassId = x.ClassId,
                Session = x.Session,
                Term = x.Term
            })
            .ToListAsync();

        var present = attendance.Count(x => x.Status == "Present");
        var absent = attendance.Count(x => x.Status == "Absent");
        var late = attendance.Count(x => x.Status == "Late");
        var excused = attendance.Count(x => x.Status == "Excused");
        var holiday = attendance.Count(x => x.Status == "Holiday");

        // Late counts as attended; Excused and Holiday don't count.
        var counted = present + absent + late;

        decimal? percentage = counted == 0
            ? null
            : Math.Round((decimal)(present + late) / counted * 100, 2);

        return (
            true,
            new
            {
                StudentId = student.Id,
                student.StudentNumber,
                ClassId = student.ClassId,
                ClassName = student.ClassName,
                Session = period.Session,
                Term = period.Term,
                Summary = new
                {
                    TotalRecords = attendance.Count,
                    Present = present,
                    Absent = absent,
                    Late = late,
                    Excused = excused,
                    Holiday = holiday,
                    AttendancePercentage = percentage
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
            string? session,
            string? term)
    {
        var denied = await CheckAccessAsync(
            userId,
            studentId,
            "You do not have access to this student's assignments.");

        if (denied != null)
        {
            return (false, null, denied);
        }

        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x => x.Id == studentId)
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
            return (false, null, "Student not found.");
        }

        var period = await ResolvePeriodAsync(student.SchoolId, session, term);

        if (period.Error != null)
        {
            return (false, null, period.Error);
        }

        var assignments = await _context.Assignments
            .AsNoTracking()
            .Where(x =>
                x.ClassId == student.ClassId &&
                x.SchoolId == student.SchoolId &&
                x.Session == period.Session &&
                x.Term == period.Term &&
                x.IsPublished)
            .OrderByDescending(x => x.AssignedAt)
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
                    .Where(s => s.StudentId == student.Id)
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

        var submitted = assignments.Count(x => x.Submission != null);

        return (
            true,
            new
            {
                StudentId = student.Id,
                student.StudentNumber,
                ClassId = student.ClassId,
                ClassName = student.ClassName,
                Session = period.Session,
                Term = period.Term,
                TotalAssignments = assignments.Count,
                Submitted = submitted,
                Pending = assignments.Count - submitted,
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
        var denied = await CheckAccessAsync(
            userId,
            studentId,
            "You do not have access to this student's assignment.");

        if (denied != null)
        {
            return (false, null, denied);
        }

        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x => x.Id == studentId)
            .Select(x => new { x.Id, x.StudentNumber, x.SchoolId, x.ClassId })
            .FirstOrDefaultAsync();

        if (student == null)
        {
            return (false, null, "Student not found.");
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
                    .Where(s => s.StudentId == student.Id)
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

        return assignment == null
            ? (false, null, "Assignment not found.")
            : (true, assignment, null);
    }
}


// ====================================================================
// IParentPortalService — change these three signatures to string?
// ====================================================================
//
// Task<(bool Success, object? Data, string? Error)> GetChildResultsAsync(
//     string userId, Guid studentId, string? session, string? term);
//
// Task<(bool Success, object? Data, string? Error)> GetChildAttendanceAsync(
//     string userId, Guid studentId, string? session, string? term);
//
// Task<(bool Success, object? Data, string? Error)> GetChildAssignmentsAsync(
//     string userId, Guid studentId, string? session, string? term);
//
//
// ====================================================================
// ParentController — in the results / attendance / assignments actions
// ====================================================================
//
// 1. Make the query parameters optional:
//        [FromQuery] string? session,
//        [FromQuery] string? term
//
// 2. Delete any "Session and term are required" checks.
//
// 3. Pass them straight to the service, which now picks the current
//    session/term when they are missing.