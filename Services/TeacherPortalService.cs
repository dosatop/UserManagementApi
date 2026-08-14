using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Results;
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