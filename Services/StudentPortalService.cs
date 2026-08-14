using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.Models.Assignments;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

public class StudentPortalService : IStudentPortalService
{
    private readonly ApplicationDbContext _context;

    public StudentPortalService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // PROFILE
    // ============================================================

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

    // ============================================================
    // CLASS
    // ============================================================

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

        return (
            true,
            student,
            null
        );
    }

    // ============================================================
    // SUBJECTS
    // ============================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetSubjectsAsync(string userId)
    {
        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new
            {
                x.Id,
                x.SchoolId,
                x.ClassId
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

        var subjects = await _context.Subjects
            .AsNoTracking()
            .Where(x => x.SchoolId == student.SchoolId)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.SchoolId
            })
            .OrderBy(x => x.Name)
            .ToListAsync();

        return (
            true,
            new
            {
                StudentId = student.Id,
                ClassId = student.ClassId,
                SchoolId = student.SchoolId,

                TotalSubjects = subjects.Count,

                Subjects = subjects
            },
            null
        );
    }

    // ============================================================
    // RESULTS
    // ============================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetResultsAsync(
            string userId,
            string session,
            string term)
    {
        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new
            {
                x.Id,
                x.StudentNumber,
                x.ClassId,
                x.SchoolId
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

                ClassId = student.ClassId,
                SchoolId = student.SchoolId,

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

    // ============================================================
    // ATTENDANCE
    // ============================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetAttendanceAsync(
            string userId,
            string session,
            string term)
    {
        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new
            {
                x.Id,
                x.StudentNumber,
                x.ClassId,
                x.SchoolId
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

        var attendance = await _context.AttendanceRecords
            .AsNoTracking()
            .Where(x =>
                x.StudentId == student.Id &&
                x.ClassId == student.ClassId &&
                x.SchoolId == student.SchoolId &&
                x.Session == session &&
                x.Term == term)
            .OrderByDescending(x => x.AttendanceDate)
            .Select(x => new
            {
                x.Id,

                x.AttendanceDate,

                x.Status,

                x.Remarks,

                SubjectId = x.SubjectId,

                SubjectName = x.Subject != null
                    ? x.Subject.Name
                    : null,

                TeacherId = x.TeacherId,

                ClassId = x.ClassId,

                SchoolId = x.SchoolId,

                x.Session,
                x.Term
            })
            .ToListAsync();

        // --------------------------------------------------------
        // ATTENDANCE SUMMARY
        // --------------------------------------------------------

        var total = attendance.Count;

        var present = attendance.Count(x =>
            x.Status == Models.Attendance.AttendanceStatus.Present);

        var absent = attendance.Count(x =>
            x.Status == Models.Attendance.AttendanceStatus.Absent);

        var late = attendance.Count(x =>
            x.Status == Models.Attendance.AttendanceStatus.Late);

        var excused = attendance.Count(x =>
            x.Status == Models.Attendance.AttendanceStatus.Excused);

        var holiday = attendance.Count(x =>
            x.Status == Models.Attendance.AttendanceStatus.Holiday);

        // Holidays should not reduce the student's attendance percentage.
        var attendanceDays = total - holiday;

        var attendancePercentage = attendanceDays == 0
            ? 0
            : Math.Round(
                (decimal)(present + late) /
                attendanceDays *
                100,
                2);

        return (
            true,
            new
            {
                StudentId = student.Id,
                StudentNumber = student.StudentNumber,

                ClassId = student.ClassId,
                SchoolId = student.SchoolId,

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

                    AttendanceDays = attendanceDays,

                    AttendancePercentage =
                        attendancePercentage
                },

                Records = attendance.Select(x => new
                {
                    x.Id,
                    x.AttendanceDate,

                    Status = x.Status.ToString(),

                    x.Remarks,

                    x.SubjectId,
                    x.SubjectName,

                    x.TeacherId,
                    x.ClassId,
                    x.SchoolId,

                    x.Session,
                    x.Term
                })
            },
            null
        );
    }

    // ============================================================
    // ASSIGNMENTS
    // ============================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetAssignmentsAsync(
            string userId,
            string session,
            string term)
    {
        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new
            {
                x.Id,
                x.StudentNumber,
                x.ClassId,
                x.SchoolId
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

        var assignments = await _context.Assignments
            .AsNoTracking()
            .Where(x =>
                x.ClassId == student.ClassId &&
                x.SchoolId == student.SchoolId &&
                x.Session == session &&
                x.Term == term &&
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

        return (
            true,
            new
            {
                StudentId = student.Id,
                StudentNumber = student.StudentNumber,

                ClassId = student.ClassId,
                SchoolId = student.SchoolId,

                Session = session,
                Term = term,

                TotalAssignments = assignments.Count,

                Assignments = assignments
            },
            null
        );
    }

    // ============================================================
    // SINGLE ASSIGNMENT
    // ============================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetAssignmentAsync(
            string userId,
            Guid assignmentId)
    {
        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new
            {
                x.Id,
                x.StudentNumber,
                x.ClassId,
                x.SchoolId
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

    // ============================================================
    // SUBMIT ASSIGNMENT
    // ============================================================

    public async Task<(bool Success, object? Data, string? Error)>
        SubmitAssignmentAsync(
            string userId,
            Guid assignmentId,
            string? submissionText,
            string? attachmentUrl)
    {
        // --------------------------------------------------------
        // FIND STUDENT
        // --------------------------------------------------------

        var student = await _context.StudentProfiles
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

        // --------------------------------------------------------
        // FIND ASSIGNMENT
        // --------------------------------------------------------

        var assignment = await _context.Assignments
            .FirstOrDefaultAsync(x =>
                x.Id == assignmentId &&
                x.ClassId == student.ClassId &&
                x.SchoolId == student.SchoolId &&
                x.IsPublished);

        if (assignment == null)
        {
            return (
                false,
                null,
                "Assignment not found."
            );
        }

        // --------------------------------------------------------
        // VALIDATE SUBMISSION
        // --------------------------------------------------------

        if (string.IsNullOrWhiteSpace(submissionText) &&
            string.IsNullOrWhiteSpace(attachmentUrl))
        {
            return (
                false,
                null,
                "Please provide submission text or an attachment."
            );
        }

        // --------------------------------------------------------
        // CHECK DUE DATE
        // --------------------------------------------------------

        var now = DateTime.UtcNow;

        var isLate = assignment.DueDate.HasValue &&
                     now > assignment.DueDate.Value;

        // --------------------------------------------------------
        // FIND EXISTING SUBMISSION
        // --------------------------------------------------------

        var submission = await _context.AssignmentSubmissions
            .FirstOrDefaultAsync(x =>
                x.AssignmentId == assignmentId &&
                x.StudentId == student.Id);

        // --------------------------------------------------------
        // CREATE NEW SUBMISSION
        // --------------------------------------------------------

        if (submission == null)
        {
            submission = new AssignmentSubmission
            {
                Id = Guid.NewGuid(),

                AssignmentId = assignmentId,
                StudentId = student.Id,

                SubmissionText = submissionText,
                AttachmentUrl = attachmentUrl,

                SubmittedAt = now,

                IsGraded = false,

                CreatedAt = now
            };

            _context.AssignmentSubmissions.Add(submission);
        }
        else
        {
            // ----------------------------------------------------
            // DO NOT EDIT GRADED SUBMISSION
            // ----------------------------------------------------

            if (submission.IsGraded)
            {
                return (
                    false,
                    null,
                    "This assignment has already been graded and cannot be resubmitted."
                );
            }

            submission.SubmissionText = submissionText;
            submission.AttachmentUrl = attachmentUrl;

            submission.SubmittedAt = now;
            submission.UpdatedAt = now;
        }

        await _context.SaveChangesAsync();

        // --------------------------------------------------------
        // RETURN SUBMISSION
        // --------------------------------------------------------

        return (
            true,
            new
            {
                submission.Id,

                submission.AssignmentId,
                submission.StudentId,

                submission.SubmissionText,
                submission.AttachmentUrl,

                submission.SubmittedAt,

                IsLate = isLate,

                submission.Score,
                submission.Feedback,

                submission.IsGraded,
                submission.GradedAt
            },
            null
        );
    }
}