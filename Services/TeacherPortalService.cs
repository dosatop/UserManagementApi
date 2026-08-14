using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Results;
using UserManagementApi.DTOs.TeacherPortal;
using UserManagementApi.Models;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

public class TeacherPortalService(
    ApplicationDbContext context) : ITeacherPortalService
{
    private readonly ApplicationDbContext _context = context;

    // ================================================================
    // PROFILE
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetProfileAsync(string userId)
    {
        var teacher = await _context.Teachers
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId)
            .Select(x => new
            {
                teacherId = x.Id,
                userId = x.UserId,
                employeeNumber = x.EmployeeNumber,

                schoolId = x.SchoolId,
                schoolName = x.School.Name,

                teacherName = x.User.FullName,
                email = x.User.Email,
                phoneNumber = x.User.PhoneNumber
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

    // ================================================================
    // TEACHER CLASSES
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetClassesAsync(string userId)
    {
        var teacher = await _context.Teachers
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId)
            .Select(x => new
            {
                x.Id,
                x.SchoolId
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

        var classes = await _context.TeacherClasses
            .AsNoTracking()
            .Where(x =>
                x.TeacherId == teacher.Id &&
                x.Class.SchoolId == teacher.SchoolId)
            .Select(x => new
            {
                classId = x.ClassId,
                className = x.Class.Name,
                schoolId = x.Class.SchoolId,

                studentCount = x.Class.Students.Count()
            })
            .OrderBy(x => x.className)
            .ToListAsync();

        return (
            true,
            classes,
            null
        );
    }

    // ================================================================
    // TEACHER SUBJECTS
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetSubjectsAsync(string userId)
    {
        var teacher = await _context.Teachers
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId)
            .Select(x => new
            {
                x.Id,
                x.SchoolId
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

        var subjects = await _context.TeacherSubjects
            .AsNoTracking()
            .Where(x =>
                x.TeacherId == teacher.Id &&
                x.Subject.SchoolId == teacher.SchoolId)
            .Select(x => new
            {
                subjectId = x.SubjectId,
                subjectName = x.Subject.Name,
                code = x.Subject.Code,
                schoolId = x.Subject.SchoolId
            })
            .OrderBy(x => x.subjectName)
            .ToListAsync();

        return (
            true,
            subjects,
            null
        );
    }

    // ================================================================
    // GET TEACHER RESULTS
    // ================================================================
    //
    // Teacher can ONLY see:
    //
    // - Results from their school
    // - Results for classes assigned to them
    // - Results for subjects assigned to them
    //
    // Filters:
    // - StudentId
    // - ClassId
    // - SubjectId
    // - Session
    // - Term
    //
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetResultsAsync(
            Guid schoolId,
            Guid teacherId,
            GetTeacherResultsRequest request)
    {
        // ============================================================
        // VERIFY TEACHER
        // ============================================================

        var teacher = await _context.Teachers
            .AsNoTracking()
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

        // ============================================================
        // BASE QUERY
        // ============================================================

        var query = _context.StudentResults
            .AsNoTracking()
            .Where(r =>
                // School
                r.SchoolId == schoolId &&

                // Student belongs to this school
                r.Student.SchoolId == schoolId &&

                // Student belongs to the result class
                r.Student.ClassId == r.ClassId &&

                // Teacher teaches this class
                r.Class.TeacherClasses.Any(tc =>
                    tc.TeacherId == teacherId) &&

                // Teacher teaches this subject
                r.Subject.TeacherSubjects.Any(ts =>
                    ts.TeacherId == teacherId)
            );

        // ============================================================
        // STUDENT FILTER
        // ============================================================

        if (request.StudentId.HasValue)
        {
            query = query.Where(r =>
                r.StudentId == request.StudentId.Value);
        }

        // ============================================================
        // CLASS FILTER
        // ============================================================

        if (request.ClassId.HasValue)
        {
            query = query.Where(r =>
                r.ClassId == request.ClassId.Value);
        }

        // ============================================================
        // SUBJECT FILTER
        // ============================================================

        if (request.SubjectId.HasValue)
        {
            query = query.Where(r =>
                r.SubjectId == request.SubjectId.Value);
        }

        // ============================================================
        // SESSION FILTER
        // ============================================================

        if (!string.IsNullOrWhiteSpace(request.Session))
        {
            query = query.Where(r =>
                r.Session == request.Session);
        }

        // ============================================================
        // TERM FILTER
        // ============================================================

        if (!string.IsNullOrWhiteSpace(request.Term))
        {
            query = query.Where(r =>
                r.Term == request.Term);
        }

        // ============================================================
        // RESULT
        // ============================================================

        var results = await query
            .OrderBy(r => r.Class.Name)
            .ThenBy(r => r.Student.User.FullName)
            .ThenBy(r => r.Subject.Name)
            .Select(r => new
            {
                resultId = r.Id,

                // ----------------------------------------------------
                // Student
                // ----------------------------------------------------

                studentId = r.StudentId,
                studentName = r.Student.User.FullName,
                studentNumber = r.Student.StudentNumber,
                studentEmail = r.Student.User.Email,
                studentPhoneNumber = r.Student.User.PhoneNumber,

                // ----------------------------------------------------
                // Class
                // ----------------------------------------------------

                classId = r.ClassId,
                className = r.Class.Name,

                // ----------------------------------------------------
                // Subject
                // ----------------------------------------------------

                subjectId = r.SubjectId,
                subjectName = r.Subject.Name,
                subjectCode = r.Subject.Code,

                // ----------------------------------------------------
                // School
                // ----------------------------------------------------

                schoolId = r.SchoolId,
                schoolName = r.School.Name,

                // ----------------------------------------------------
                // Academic
                // ----------------------------------------------------

                session = r.Session,
                term = r.Term,

                // ----------------------------------------------------
                // Scores
                // ----------------------------------------------------

                score = r.Score,
                examScore = r.ExamScore,
                testScore = r.TestScore,

                // ----------------------------------------------------
                // Result
                // ----------------------------------------------------

                grade = r.Grade,
                remark = r.Remark,

                createdAt = r.CreatedAt
            })
            .ToListAsync();

        return (
            true,
            results,
            null
        );
    }

    // ================================================================
    // CREATE RESULT
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        CreateResultAsync(
            string userId,
            CreateResultRequest request)
    {
        // ============================================================
        // GET TEACHER
        // ============================================================

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

        var schoolId = teacher.SchoolId;

        // ============================================================
        // CURRENT ACADEMIC PERIOD
        // ============================================================

        var currentAcademicPeriod =
            await _context.AcademicSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.SchoolId == schoolId &&
                    x.IsCurrent);

        if (currentAcademicPeriod == null)
        {
            return (
                false,
                null,
                "There is no active academic session."
            );
        }

        var currentSession =
            currentAcademicPeriod.Session;

        var currentTerm =
            currentAcademicPeriod.Term;

        // ============================================================
        // TEACHER CANNOT CHOOSE ANOTHER SESSION
        // ============================================================

        if (!string.Equals(
                request.Session,
                currentSession,
                StringComparison.OrdinalIgnoreCase))
        {
            return (
                false,
                null,
                $"Results can only be uploaded for the current session: {currentSession}."
            );
        }

        // ============================================================
        // TEACHER CANNOT CHOOSE ANOTHER TERM
        // ============================================================

        if (!string.Equals(
                request.Term,
                currentTerm,
                StringComparison.OrdinalIgnoreCase))
        {
            return (
                false,
                null,
                $"Results can only be uploaded for the current term: {currentTerm}."
            );
        }

        // ============================================================
        // CHECK STUDENT
        // ============================================================

        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x =>
                x.Id == request.StudentId &&
                x.SchoolId == schoolId)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.SchoolId,
                x.ClassId,

                studentName = x.User.FullName,
                studentNumber = x.StudentNumber,

                className = x.Class.Name
            })
            .FirstOrDefaultAsync();

        if (student == null)
        {
            return (
                false,
                null,
                "Student not found in this school."
            );
        }

        // ============================================================
        // STUDENT MUST BELONG TO REQUESTED CLASS
        // ============================================================

        if (student.ClassId != request.ClassId)
        {
            return (
                false,
                null,
                "The selected class does not belong to this student."
            );
        }

        // ============================================================
        // TEACHER MUST TEACH THIS CLASS
        // ============================================================

        var teachesClass =
            await _context.TeacherClasses
                .AnyAsync(x =>
                    x.TeacherId == teacher.Id &&
                    x.ClassId == request.ClassId);

        if (!teachesClass)
        {
            return (
                false,
                null,
                "You are not assigned to this class."
            );
        }

        // ============================================================
        // CHECK SUBJECT
        // ============================================================

        var subject = await _context.Subjects
            .AsNoTracking()
            .Where(x =>
                x.Id == request.SubjectId &&
                x.SchoolId == schoolId)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Code
            })
            .FirstOrDefaultAsync();

        if (subject == null)
        {
            return (
                false,
                null,
                "Subject not found in this school."
            );
        }

        // ============================================================
        // TEACHER MUST TEACH THIS SUBJECT
        // ============================================================

        var teachesSubject =
            await _context.TeacherSubjects
                .AnyAsync(x =>
                    x.TeacherId == teacher.Id &&
                    x.SubjectId == request.SubjectId);

        if (!teachesSubject)
        {
            return (
                false,
                null,
                "You are not assigned to this subject."
            );
        }

        // ============================================================
        // CHECK DUPLICATE
        // ============================================================

        var exists = await _context.StudentResults
            .AnyAsync(x =>
                x.StudentId == request.StudentId &&
                x.SubjectId == request.SubjectId &&
                x.ClassId == request.ClassId &&
                x.Session == currentSession &&
                x.Term == currentTerm &&
                x.SchoolId == schoolId);

        if (exists)
        {
            return (
                false,
                null,
                "A result already exists for this student, subject, session and term."
            );
        }

        // ============================================================
        // VALIDATE SCORE
        // ============================================================

        if (request.Score < 0 || request.Score > 100)
        {
            return (
                false,
                null,
                "Score must be between 0 and 100."
            );
        }

        // ============================================================
        // CALCULATE GRADE
        // ============================================================

        var grade = CalculateGrade(request.Score);

        // ============================================================
        // CREATE
        // ============================================================

        var result = new Models.Results.StudentResult
        {
            Id = Guid.NewGuid(),

            StudentId = student.Id,
            SchoolId = schoolId,
            SubjectId = subject.Id,
            ClassId = student.ClassId,

            Session = currentSession,
            Term = currentTerm,

            Score = request.Score,
            ExamScore = request.ExamScore,
            TestScore = request.TestScore,

            Grade = grade,
            Remark = request.Remark,

            CreatedAt = DateTime.UtcNow
        };

        _context.StudentResults.Add(result);

        await _context.SaveChangesAsync();

        // ============================================================
        // RESPONSE
        // ============================================================

        return (
            true,
            new
            {
                resultId = result.Id,

                schoolId,

                studentId = student.Id,
                studentName = student.studentName,
                studentNumber = student.studentNumber,

                classId = student.ClassId,
                className = student.className,

                subjectId = subject.Id,
                subjectName = subject.Name,
                subjectCode = subject.Code,

                session = result.Session,
                term = result.Term,

                score = result.Score,
                examScore = result.ExamScore,
                testScore = result.TestScore,

                grade = result.Grade,
                remark = result.Remark,

                createdAt = result.CreatedAt
            },
            null
        );
    }

    // ================================================================
    // UPDATE RESULT
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        UpdateResultAsync(
            string userId,
            Guid resultId,
            CreateResultRequest request)
    {
        // ============================================================
        // GET TEACHER
        // ============================================================

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

        var schoolId = teacher.SchoolId;

        // ============================================================
        // CURRENT PERIOD
        // ============================================================

        var currentAcademicPeriod =
            await _context.AcademicSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.SchoolId == schoolId &&
                    x.IsCurrent);

        if (currentAcademicPeriod == null)
        {
            return (
                false,
                null,
                "There is no active academic session."
            );
        }

        var currentSession =
            currentAcademicPeriod.Session;

        var currentTerm =
            currentAcademicPeriod.Term;

        // ============================================================
        // GET RESULT
        // ============================================================

        var result = await _context.StudentResults
            .FirstOrDefaultAsync(x =>
                x.Id == resultId &&
                x.SchoolId == schoolId);

        if (result == null)
        {
            return (
                false,
                null,
                "Result not found."
            );
        }

        // ============================================================
        // RESULT MUST BE CURRENT
        // ============================================================

        if (result.Session != currentSession ||
            result.Term != currentTerm)
        {
            return (
                false,
                null,
                "Only results for the current term can be edited."
            );
        }

        // ============================================================
        // TEACHER MUST TEACH ORIGINAL CLASS
        // ============================================================

        var teachesOriginalClass =
            await _context.TeacherClasses
                .AnyAsync(x =>
                    x.TeacherId == teacher.Id &&
                    x.ClassId == result.ClassId);

        if (!teachesOriginalClass)
        {
            return (
                false,
                null,
                "You are not assigned to this class."
            );
        }

        // ============================================================
        // TEACHER MUST TEACH ORIGINAL SUBJECT
        // ============================================================

        var teachesOriginalSubject =
            await _context.TeacherSubjects
                .AnyAsync(x =>
                    x.TeacherId == teacher.Id &&
                    x.SubjectId == result.SubjectId);

        if (!teachesOriginalSubject)
        {
            return (
                false,
                null,
                "You are not assigned to this subject."
            );
        }

        // ============================================================
        // GET NEW STUDENT
        // ============================================================

        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x =>
                x.Id == request.StudentId &&
                x.SchoolId == schoolId)
            .Select(x => new
            {
                x.Id,
                x.ClassId,

                studentName = x.User.FullName,
                studentNumber = x.StudentNumber,
                className = x.Class.Name
            })
            .FirstOrDefaultAsync();

        if (student == null)
        {
            return (
                false,
                null,
                "Student not found in this school."
            );
        }

        // ============================================================
        // STUDENT MUST BELONG TO CLASS
        // ============================================================

        if (student.ClassId != request.ClassId)
        {
            return (
                false,
                null,
                "The selected class does not belong to this student."
            );
        }

        // ============================================================
        // NEW CLASS MUST BE TAUGHT BY TEACHER
        // ============================================================

        var newClassAllowed =
            await _context.TeacherClasses
                .AnyAsync(x =>
                    x.TeacherId == teacher.Id &&
                    x.ClassId == request.ClassId);

        if (!newClassAllowed)
        {
            return (
                false,
                null,
                "You are not assigned to this class."
            );
        }

        // ============================================================
        // NEW SUBJECT MUST BE TAUGHT BY TEACHER
        // ============================================================

        var newSubjectAllowed =
            await _context.TeacherSubjects
                .AnyAsync(x =>
                    x.TeacherId == teacher.Id &&
                    x.SubjectId == request.SubjectId);

        if (!newSubjectAllowed)
        {
            return (
                false,
                null,
                "You are not assigned to this subject."
            );
        }

        // ============================================================
        // SUBJECT MUST BELONG TO SCHOOL
        // ============================================================

        var subject = await _context.Subjects
            .AsNoTracking()
            .Where(x =>
                x.Id == request.SubjectId &&
                x.SchoolId == schoolId)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Code
            })
            .FirstOrDefaultAsync();

        if (subject == null)
        {
            return (
                false,
                null,
                "Subject not found in this school."
            );
        }

        // ============================================================
        // SCORE VALIDATION
        // ============================================================

        if (request.Score < 0 || request.Score > 100)
        {
            return (
                false,
                null,
                "Score must be between 0 and 100."
            );
        }

        // ============================================================
        // DUPLICATE
        // ============================================================

        var duplicate =
            await _context.StudentResults
                .AnyAsync(x =>
                    x.Id != resultId &&
                    x.SchoolId == schoolId &&
                    x.StudentId == request.StudentId &&
                    x.SubjectId == request.SubjectId &&
                    x.ClassId == request.ClassId &&
                    x.Session == currentSession &&
                    x.Term == currentTerm);

        if (duplicate)
        {
            return (
                false,
                null,
                "A result already exists for this student, subject and term."
            );
        }

        // ============================================================
        // UPDATE
        // ============================================================

        result.StudentId = request.StudentId;
        result.SubjectId = request.SubjectId;
        result.ClassId = request.ClassId;

        // Never allow the request to change these.
        result.Session = currentSession;
        result.Term = currentTerm;

        result.Score = request.Score;
        result.ExamScore = request.ExamScore;
        result.TestScore = request.TestScore;
        result.Remark = request.Remark;

        result.Grade = CalculateGrade(request.Score);

        await _context.SaveChangesAsync();

        // ============================================================
        // RESPONSE
        // ============================================================

        return (
            true,
            new
            {
                resultId = result.Id,

                schoolId,

                studentId = result.StudentId,
                studentName = student.studentName,
                studentNumber = student.studentNumber,

                classId = result.ClassId,
                className = student.className,

                subjectId = result.SubjectId,
                subjectName = subject.Name,
                subjectCode = subject.Code,

                session = result.Session,
                term = result.Term,

                score = result.Score,
                examScore = result.ExamScore,
                testScore = result.TestScore,

                grade = result.Grade,
                remark = result.Remark,

                createdAt = result.CreatedAt
            },
            null
        );
    }

    // ================================================================
    // DELETE RESULT
    // ================================================================

    public async Task<(bool Success, string? Error)>
        DeleteResultAsync(
            string userId,
            Guid resultId)
    {
        // ============================================================
        // GET TEACHER
        // ============================================================

        var teacher = await _context.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.UserId == userId);

        if (teacher == null)
        {
            return (
                false,
                "Teacher profile not found."
            );
        }

        var schoolId = teacher.SchoolId;

        // ============================================================
        // CURRENT PERIOD
        // ============================================================

        var currentAcademicPeriod =
            await _context.AcademicSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.SchoolId == schoolId &&
                    x.IsCurrent);

        if (currentAcademicPeriod == null)
        {
            return (
                false,
                "There is no active academic session."
            );
        }

        // ============================================================
        // GET RESULT
        // ============================================================

        var result = await _context.StudentResults
            .FirstOrDefaultAsync(x =>
                x.Id == resultId &&
                x.SchoolId == schoolId);

        if (result == null)
        {
            return (
                false,
                "Result not found."
            );
        }

        // ============================================================
        // ONLY CURRENT TERM
        // ============================================================

        if (result.Session != currentAcademicPeriod.Session ||
            result.Term != currentAcademicPeriod.Term)
        {
            return (
                false,
                "Only results for the current term can be deleted."
            );
        }

        // ============================================================
        // TEACHER MUST TEACH CLASS
        // ============================================================

        var teachesClass =
            await _context.TeacherClasses
                .AnyAsync(x =>
                    x.TeacherId == teacher.Id &&
                    x.ClassId == result.ClassId);

        if (!teachesClass)
        {
            return (
                false,
                "You are not assigned to this class."
            );
        }

        // ============================================================
        // TEACHER MUST TEACH SUBJECT
        // ============================================================

        var teachesSubject =
            await _context.TeacherSubjects
                .AnyAsync(x =>
                    x.TeacherId == teacher.Id &&
                    x.SubjectId == result.SubjectId);

        if (!teachesSubject)
        {
            return (
                false,
                "You are not assigned to this subject."
            );
        }

        // ============================================================
        // DELETE
        // ============================================================

        _context.StudentResults.Remove(result);

        await _context.SaveChangesAsync();

        return (
            true,
            null
        );
    }

    // ================================================================
    // ASSIGNMENTS
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        CreateAssignmentAsync(
            string userId,
            CreateAssignmentRequest request)
    {
        // ============================================================
        // GET TEACHER
        // ============================================================

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

        // ============================================================
        // CURRENT ACADEMIC PERIOD
        // ============================================================

        var period = await _context.AcademicSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.SchoolId == teacher.SchoolId &&
                x.IsCurrent);

        if (period == null)
        {
            return (
                false,
                null,
                "There is no active academic session."
            );
        }

        // ============================================================
        // VALIDATE CLASS
        // ============================================================

        var classExists = await _context.TeacherClasses
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.ClassId == request.ClassId &&
                x.Class.SchoolId == teacher.SchoolId);

        if (!classExists)
        {
            return (
                false,
                null,
                "You are not assigned to this class."
            );
        }

        // ============================================================
        // VALIDATE SUBJECT
        // ============================================================

        var subjectExists = await _context.TeacherSubjects
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.SubjectId == request.SubjectId &&
                x.Subject.SchoolId == teacher.SchoolId);

        if (!subjectExists)
        {
            return (
                false,
                null,
                "You are not assigned to this subject."
            );
        }

        // ============================================================
        // VALIDATION
        // ============================================================

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return (
                false,
                null,
                "Assignment title is required."
            );
        }

        // ============================================================
        // NORMALIZE DUE DATE
        // ============================================================

        DateTime? dueDateUtc = null;

        if (request.DueDate.HasValue)
        {
            dueDateUtc = request.DueDate.Value.Kind switch
            {
                DateTimeKind.Utc =>
                    request.DueDate.Value,

                DateTimeKind.Local =>
                    request.DueDate.Value.ToUniversalTime(),

                DateTimeKind.Unspecified =>
                    DateTime.SpecifyKind(
                        request.DueDate.Value,
                        DateTimeKind.Utc
                    ),

                _ => request.DueDate.Value
            };
        }

        // ============================================================
        // CREATE
        // ============================================================

        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),

            SchoolId = teacher.SchoolId,
            TeacherId = teacher.Id,

            ClassId = request.ClassId,
            SubjectId = request.SubjectId,

            Title = request.Title.Trim(),
            Description = request.Description,
            AttachmentUrl = request.AttachmentUrl,

            Session = period.Session,
            Term = period.Term,

            AssignedAt = DateTime.UtcNow,
            DueDate = dueDateUtc,

            IsPublished = true,

            CreatedAt = DateTime.UtcNow
        };

        _context.Assignments.Add(assignment);

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                assignmentId = assignment.Id,
                assignment.SchoolId,
                assignment.TeacherId,
                assignment.ClassId,
                assignment.SubjectId,
                assignment.Title,
                assignment.Description,
                assignment.AttachmentUrl,
                assignment.Session,
                assignment.Term,
                assignment.AssignedAt,
                assignment.DueDate,
                assignment.IsPublished
            },
            null
        );
    }


    // ================================================================
    // GET ASSIGNMENTS
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetAssignmentsAsync(
            string userId,
            GetTeacherAssignmentsRequest request)
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

        var query = _context.Assignments
            .AsNoTracking()
            .Where(x =>
                x.TeacherId == teacher.Id &&
                x.SchoolId == teacher.SchoolId);

        if (request.ClassId.HasValue)
        {
            query = query.Where(x =>
                x.ClassId == request.ClassId.Value);
        }

        if (request.SubjectId.HasValue)
        {
            query = query.Where(x =>
                x.SubjectId == request.SubjectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Session))
        {
            query = query.Where(x =>
                x.Session == request.Session);
        }

        if (!string.IsNullOrWhiteSpace(request.Term))
        {
            query = query.Where(x =>
                x.Term == request.Term);
        }

        var assignments = await query
            .OrderByDescending(x => x.AssignedAt)
            .Select(x => new
            {
                assignmentId = x.Id,

                x.Title,
                x.Description,
                x.AttachmentUrl,

                x.ClassId,
                className = x.Class.Name,

                x.SubjectId,
                subjectName = x.Subject.Name,

                x.Session,
                x.Term,

                x.AssignedAt,
                x.DueDate,

                x.IsPublished,

                submissionCount = x.Submissions.Count()
            })
            .ToListAsync();

        return (
            true,
            assignments,
            null
        );
    }


    // ================================================================
    // GET SINGLE ASSIGNMENT
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetAssignmentAsync(
            string userId,
            Guid assignmentId)
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

        var assignment = await _context.Assignments
            .AsNoTracking()
            .Where(x =>
                x.Id == assignmentId &&
                x.TeacherId == teacher.Id &&
                x.SchoolId == teacher.SchoolId)
            .Select(x => new
            {
                assignmentId = x.Id,

                x.Title,
                x.Description,
                x.AttachmentUrl,

                x.ClassId,
                className = x.Class.Name,

                x.SubjectId,
                subjectName = x.Subject.Name,

                x.Session,
                x.Term,

                x.AssignedAt,
                x.DueDate,

                x.IsPublished,

                Submissions = x.Submissions
                    .Select(s => new
                    {
                        submissionId = s.Id,

                        studentId = s.StudentId,
                        studentName = s.Student.User.FullName,
                        studentNumber = s.Student.StudentNumber,

                        s.SubmissionText,
                        s.AttachmentUrl,
                        s.SubmittedAt,

                        s.Score,
                        s.Feedback,
                        s.IsGraded,
                        s.GradedAt
                    })
                    .OrderBy(s => s.studentName)
                    .ToList()
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


    // ================================================================
    // UPDATE ASSIGNMENT
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        UpdateAssignmentAsync(
            string userId,
            Guid assignmentId,
            UpdateAssignmentRequest request)
    {
        var teacher = await _context.Teachers
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

        var assignment = await _context.Assignments
            .FirstOrDefaultAsync(x =>
                x.Id == assignmentId &&
                x.TeacherId == teacher.Id &&
                x.SchoolId == teacher.SchoolId);

        if (assignment == null)
        {
            return (
                false,
                null,
                "Assignment not found."
            );
        }

        // ============================================================
        // CURRENT PERIOD
        // ============================================================

        var period = await _context.AcademicSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.SchoolId == teacher.SchoolId &&
                x.IsCurrent);

        if (period == null)
        {
            return (
                false,
                null,
                "There is no active academic session."
            );
        }

        if (assignment.Session != period.Session ||
            assignment.Term != period.Term)
        {
            return (
                false,
                null,
                "Only assignments from the current academic period can be edited."
            );
        }

        // ============================================================
        // CLASS
        // ============================================================

        var teachesClass = await _context.TeacherClasses
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.ClassId == request.ClassId &&
                x.Class.SchoolId == teacher.SchoolId);

        if (!teachesClass)
        {
            return (
                false,
                null,
                "You are not assigned to this class."
            );
        }

        // ============================================================
        // SUBJECT
        // ============================================================

        var teachesSubject = await _context.TeacherSubjects
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.SubjectId == request.SubjectId &&
                x.Subject.SchoolId == teacher.SchoolId);

        if (!teachesSubject)
        {
            return (
                false,
                null,
                "You are not assigned to this subject."
            );
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return (
                false,
                null,
                "Assignment title is required."
            );
        }

        assignment.ClassId = request.ClassId;
        assignment.SubjectId = request.SubjectId;

        assignment.Title = request.Title.Trim();
        assignment.Description = request.Description;
        assignment.AttachmentUrl = request.AttachmentUrl;
        assignment.DueDate = request.DueDate;

        assignment.IsPublished = request.IsPublished;
        assignment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                assignmentId = assignment.Id,
                assignment.Title,
                assignment.Description,
                assignment.AttachmentUrl,
                assignment.ClassId,
                assignment.SubjectId,
                assignment.Session,
                assignment.Term,
                assignment.AssignedAt,
                assignment.DueDate,
                assignment.IsPublished,
                assignment.UpdatedAt
            },
            null
        );
    }


    // ================================================================
    // DELETE ASSIGNMENT
    // ================================================================

    public async Task<(bool Success, string? Error)>
        DeleteAssignmentAsync(
            string userId,
            Guid assignmentId)
    {
        var teacher = await _context.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.UserId == userId);

        if (teacher == null)
        {
            return (
                false,
                "Teacher profile not found."
            );
        }

        var assignment = await _context.Assignments
            .FirstOrDefaultAsync(x =>
                x.Id == assignmentId &&
                x.TeacherId == teacher.Id &&
                x.SchoolId == teacher.SchoolId);

        if (assignment == null)
        {
            return (
                false,
                "Assignment not found."
            );
        }

        var period = await _context.AcademicSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.SchoolId == teacher.SchoolId &&
                x.IsCurrent);

        if (period == null)
        {
            return (
                false,
                "There is no active academic session."
            );
        }

        if (assignment.Session != period.Session ||
            assignment.Term != period.Term)
        {
            return (
                false,
                "Only assignments from the current academic period can be deleted."
            );
        }

        _context.Assignments.Remove(assignment);

        await _context.SaveChangesAsync();

        return (
            true,
            null
        );
    }

    // ================================================================
    // ATTENDANCE
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        CreateAttendanceAsync(
            string userId,
            CreateAttendanceRequest request)
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

        // ============================================================
        // CURRENT PERIOD
        // ============================================================

        var period = await _context.AcademicSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.SchoolId == teacher.SchoolId &&
                x.IsCurrent);

        if (period == null)
        {
            return (
                false,
                null,
                "There is no active academic session."
            );
        }

        // ============================================================
        // TEACHER MUST TEACH CLASS
        // ============================================================

        var teachesClass = await _context.TeacherClasses
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.ClassId == request.ClassId &&
                x.Class.SchoolId == teacher.SchoolId);

        if (!teachesClass)
        {
            return (
                false,
                null,
                "You are not assigned to this class."
            );
        }

        // ============================================================
        // STUDENT
        // ============================================================

        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x =>
                x.Id == request.StudentId &&
                x.SchoolId == teacher.SchoolId)
            .Select(x => new
            {
                x.Id,
                x.StudentNumber,
                x.ClassId,
                studentName = x.User.FullName
            })
            .FirstOrDefaultAsync();

        if (student == null)
        {
            return (
                false,
                null,
                "Student not found in this school."
            );
        }

        if (student.ClassId != request.ClassId)
        {
            return (
                false,
                null,
                "Student does not belong to this class."
            );
        }

        // ============================================================
        // SUBJECT
        // ============================================================

        if (request.SubjectId.HasValue)
        {
            var teachesSubject =
                await _context.TeacherSubjects
                    .AnyAsync(x =>
                        x.TeacherId == teacher.Id &&
                        x.SubjectId == request.SubjectId.Value &&
                        x.Subject.SchoolId == teacher.SchoolId);

            if (!teachesSubject)
            {
                return (
                    false,
                    null,
                    "You are not assigned to this subject."
                );
            }
        }

        // ============================================================
        // DUPLICATE
        // ============================================================

        var date = request.AttendanceDate.Date;

        var duplicate = await _context.AttendanceRecords
            .AnyAsync(x =>
                x.StudentId == request.StudentId &&
                x.ClassId == request.ClassId &&
                x.SubjectId == request.SubjectId &&
                x.AttendanceDate.Date == date &&
                x.Session == period.Session &&
                x.Term == period.Term);

        if (duplicate)
        {
            return (
                false,
                null,
                "Attendance has already been recorded for this student on this date."
            );
        }

        // ============================================================
        // CREATE
        // ============================================================

        var attendance = new AttendanceRecord
        {
            Id = Guid.NewGuid(),

            SchoolId = teacher.SchoolId,

            StudentId = student.Id,
            ClassId = request.ClassId,

            TeacherId = teacher.Id,

            SubjectId = request.SubjectId,

            AttendanceDate = date,

            Status = request.Status,

            Remarks = request.Remarks,

            Session = period.Session,
            Term = period.Term,

            CreatedAt = DateTime.UtcNow
        };

        _context.AttendanceRecords.Add(attendance);

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                attendanceId = attendance.Id,

                studentId = student.Id,
                studentName = student.studentName,
                studentNumber = student.StudentNumber,

                attendance.ClassId,
                attendance.SubjectId,

                attendance.AttendanceDate,

                status = attendance.Status.ToString(),

                attendance.Remarks,

                attendance.Session,
                attendance.Term,

                attendance.TeacherId,

                attendance.CreatedAt
            },
            null
        );
    }


    // ================================================================
    // GET ATTENDANCE
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetAttendanceAsync(
            string userId,
            GetTeacherAttendanceRequest request)
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

        var query = _context.AttendanceRecords
            .AsNoTracking()
            .Where(x =>
                x.TeacherId == teacher.Id &&
                x.SchoolId == teacher.SchoolId);

        if (request.StudentId.HasValue)
        {
            query = query.Where(x =>
                x.StudentId == request.StudentId.Value);
        }

        if (request.ClassId.HasValue)
        {
            query = query.Where(x =>
                x.ClassId == request.ClassId.Value);
        }

        if (request.SubjectId.HasValue)
        {
            query = query.Where(x =>
                x.SubjectId == request.SubjectId.Value);
        }

        if (request.Date.HasValue)
        {
            var date = request.Date.Value.Date;

            query = query.Where(x =>
                x.AttendanceDate.Date == date);
        }

        if (!string.IsNullOrWhiteSpace(request.Session))
        {
            query = query.Where(x =>
                x.Session == request.Session);
        }

        if (!string.IsNullOrWhiteSpace(request.Term))
        {
            query = query.Where(x =>
                x.Term == request.Term);
        }

        var records = await query
            .OrderByDescending(x => x.AttendanceDate)
            .ThenBy(x => x.Student.User.FullName)
            .Select(x => new
            {
                attendanceId = x.Id,

                studentId = x.StudentId,
                studentName = x.Student.User.FullName,
                studentNumber = x.Student.StudentNumber,

                classId = x.ClassId,
                className = x.Class.Name,

                subjectId = x.SubjectId,
                subjectName = x.Subject != null
                    ? x.Subject.Name
                    : null,

                attendanceDate = x.AttendanceDate,

                status = x.Status.ToString(),

                x.Remarks,

                x.Session,
                x.Term
            })
            .ToListAsync();

        return (
            true,
            records,
            null
        );
    }


    // ================================================================
    // GET SINGLE ATTENDANCE
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetAttendanceRecordAsync(
            string userId,
            Guid attendanceId)
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

        var record = await _context.AttendanceRecords
            .AsNoTracking()
            .Where(x =>
                x.Id == attendanceId &&
                x.TeacherId == teacher.Id &&
                x.SchoolId == teacher.SchoolId)
            .Select(x => new
            {
                attendanceId = x.Id,

                studentId = x.StudentId,
                studentName = x.Student.User.FullName,
                studentNumber = x.Student.StudentNumber,

                classId = x.ClassId,
                className = x.Class.Name,

                subjectId = x.SubjectId,
                subjectName = x.Subject != null
                    ? x.Subject.Name
                    : null,

                attendanceDate = x.AttendanceDate,

                status = x.Status.ToString(),

                x.Remarks,

                x.Session,
                x.Term,

                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (record == null)
        {
            return (
                false,
                null,
                "Attendance record not found."
            );
        }

        return (
            true,
            record,
            null
        );
    }


    // ================================================================
    // UPDATE ATTENDANCE
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        UpdateAttendanceAsync(
            string userId,
            Guid attendanceId,
            UpdateAttendanceRequest request)
    {
        var teacher = await _context.Teachers
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

        var record = await _context.AttendanceRecords
            .FirstOrDefaultAsync(x =>
                x.Id == attendanceId &&
                x.TeacherId == teacher.Id &&
                x.SchoolId == teacher.SchoolId);

        if (record == null)
        {
            return (
                false,
                null,
                "Attendance record not found."
            );
        }

        var period = await _context.AcademicSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.SchoolId == teacher.SchoolId &&
                x.IsCurrent);

        if (period == null)
        {
            return (
                false,
                null,
                "There is no active academic session."
            );
        }

        if (record.Session != period.Session ||
            record.Term != period.Term)
        {
            return (
                false,
                null,
                "Only attendance from the current academic period can be edited."
            );
        }

        record.Status = request.Status;
        record.Remarks = request.Remarks;
        record.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                attendanceId = record.Id,

                studentId = record.StudentId,
                classId = record.ClassId,
                subjectId = record.SubjectId,

                attendanceDate = record.AttendanceDate,

                status = record.Status.ToString(),

                record.Remarks,

                record.Session,
                record.Term,

                record.UpdatedAt
            },
            null
        );
    }


    // ================================================================
    // DELETE ATTENDANCE
    // ================================================================

    public async Task<(bool Success, string? Error)>
        DeleteAttendanceAsync(
            string userId,
            Guid attendanceId)
    {
        var teacher = await _context.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.UserId == userId);

        if (teacher == null)
        {
            return (
                false,
                "Teacher profile not found."
            );
        }

        var record = await _context.AttendanceRecords
            .FirstOrDefaultAsync(x =>
                x.Id == attendanceId &&
                x.TeacherId == teacher.Id &&
                x.SchoolId == teacher.SchoolId);

        if (record == null)
        {
            return (
                false,
                "Attendance record not found."
            );
        }

        var period = await _context.AcademicSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.SchoolId == teacher.SchoolId &&
                x.IsCurrent);

        if (period == null)
        {
            return (
                false,
                "There is no active academic session."
            );
        }

        if (record.Session != period.Session ||
            record.Term != period.Term)
        {
            return (
                false,
                "Only attendance from the current academic period can be deleted."
            );
        }

        _context.AttendanceRecords.Remove(record);

        await _context.SaveChangesAsync();

        return (
            true,
            null
        );
    }

    // ================================================================
    // GRADE CALCULATOR
    // ================================================================

    private static string CalculateGrade(decimal score)
    {
        return score switch
        {
            >= 70 => "A",
            >= 60 => "B",
            >= 50 => "C",
            >= 45 => "D",
            >= 40 => "E",
            _ => "F"
        };
    }
}