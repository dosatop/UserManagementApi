using System.Globalization;
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
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (teacher == null)
        {
            return (
                false,
                null,
                "Teacher profile not found."
            );
        }

        var teacherSubjects = await _context.TeacherSubjects
            .AsNoTracking()
            .Where(x => x.TeacherId == teacher.Id)
            .Select(x => new
            {
                subjectId = x.SubjectId,
                subjectName = x.Subject.Name,
                code = x.Subject.Code,

                classId = x.ClassId,
                className = x.Class.Name,

                schoolId = x.SchoolId
            })
            .ToListAsync();

        return (
            true,
            teacherSubjects,
            null
        );
    }


    public async Task<(bool Success, object? Data, string? Error)>
 GetStudentsAsync(
 string userId,
 Guid? classId = null)
    {
        // ------------------------------------------------------------
        // GET TEACHER
        // ------------------------------------------------------------

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

        // ------------------------------------------------------------
        // VERIFY CLASS FILTER
        // ------------------------------------------------------------

        if (classId.HasValue)
        {
            var teachesClass = await _context.TeacherSubjects
                .AsNoTracking()
                .AnyAsync(x =>
                    x.TeacherId == teacher.Id &&
                    x.SchoolId == teacher.SchoolId &&
                    x.ClassId == classId.Value);

            if (!teachesClass)
            {
                return (
                    false,
                    null,
                    "You are not assigned to teach this class."
                );
            }
        }

        // ------------------------------------------------------------
        // GET STUDENTS
        // ------------------------------------------------------------

        var query = _context.StudentProfiles
            .AsNoTracking()
            .Where(student =>
                student.SchoolId == teacher.SchoolId &&

                _context.TeacherSubjects.Any(assignment =>
                    assignment.TeacherId == teacher.Id &&
                    assignment.SchoolId == teacher.SchoolId &&
                    assignment.ClassId == student.ClassId)
            );

        // ------------------------------------------------------------
        // OPTIONAL CLASS FILTER
        // ------------------------------------------------------------

        if (classId.HasValue)
        {
            query = query.Where(student =>
                student.ClassId == classId.Value);
        }

        // ------------------------------------------------------------
        // RESULT
        // ------------------------------------------------------------

        var students = await query
            .Select(student => new
            {
                studentId = student.Id,
                studentName = student.User.FullName,
                studentNumber = student.StudentNumber,

                email = student.User.Email,
                phoneNumber = student.User.PhoneNumber,

                classId = student.ClassId,
                className = student.Class.Name,

                schoolId = student.SchoolId
            })
            .OrderBy(student => student.className)
            .ThenBy(student => student.studentName)
            .ToListAsync();

        return (
            true,
            new
            {
                studentCount = students.Count,

                classId,

                students
            },
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
    // BULK EXAM RESULT
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        BulkExamResultAsync(
            string userId,
            BulkExamResultRequest request)
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

        var period = await _context.AcademicSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.SchoolId == schoolId &&
                x.IsCurrent);

        if (period == null)
        {
            return (
                false,
                null,
                "There is no active academic session."
            );
        }

        var currentSession = period.Session;
        var currentTerm = await _context.AcademicTerms
    .AsNoTracking()
    .FirstOrDefaultAsync(x =>
        x.AcademicSessionId == period.Id &&
        x.IsCurrent);

        if (currentTerm == null)
        {
            return (
                false,
                null,
                "There is no active academic term."
            );
        }
        // ============================================================
        // VALIDATE CLASS
        // ============================================================

        var classExists = await _context.TeacherClasses
            .AsNoTracking()
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.ClassId == request.ClassId &&
                x.Class.SchoolId == schoolId);

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
        // TEACHER MUST TEACH SUBJECT
        // ============================================================

        var teachesSubject = await _context.TeacherSubjects
            .AsNoTracking()
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.SubjectId == request.SubjectId &&
                x.Subject.SchoolId == schoolId);

        if (!teachesSubject)
        {
            return (
                false,
                null,
                "You are not assigned to this subject."
            );
        }

        // ============================================================
        // VALIDATE RESULTS
        // ============================================================

        if (request.Results == null ||
            request.Results.Count == 0)
        {
            return (
                false,
                null,
                "At least one student result is required."
            );
        }

        // ============================================================
        // CHECK DUPLICATE STUDENTS IN REQUEST
        // ============================================================

        var duplicateStudentIds = request.Results
            .GroupBy(x => x.StudentId)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (duplicateStudentIds.Count > 0)
        {
            return (
                false,
                null,
                "The same student cannot appear more than once in the upload."
            );
        }

        // ============================================================
        // VALIDATE EXAM SCORES
        // ============================================================

        var invalidScore = request.Results
            .FirstOrDefault(x =>
                x.ExamScore < 0 ||
                x.ExamScore > 60);

        if (invalidScore != null)
        {
            return (
                false,
                null,
                $"Exam score for student {invalidScore.StudentId} must be between 0 and 60."
            );
        }

        // ============================================================
        // STUDENT IDS
        // ============================================================

        var studentIds = request.Results
            .Select(x => x.StudentId)
            .ToList();

        // ============================================================
        // GET STUDENTS
        // ============================================================

        var students = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x =>
                studentIds.Contains(x.Id) &&
                x.SchoolId == schoolId &&
                x.ClassId == request.ClassId)
            .Select(x => new
            {
                x.Id,
                x.ClassId,

                studentName = x.User.FullName,
                studentNumber = x.StudentNumber
            })
            .ToListAsync();

        // ============================================================
        // CHECK ALL STUDENTS EXIST IN CLASS
        // ============================================================

        var foundStudentIds = students
            .Select(x => x.Id)
            .ToHashSet();

        var invalidStudents = studentIds
            .Where(x => !foundStudentIds.Contains(x))
            .ToList();

        if (invalidStudents.Count > 0)
        {
            return (
                false,
                null,
                "One or more students were not found in the selected class."
            );
        }

        // ============================================================
        // GET EXISTING RESULTS
        // ============================================================

        var existingResults = await _context.StudentResults
            .Where(x =>
                x.SchoolId == schoolId &&
                x.ClassId == request.ClassId &&
                x.SubjectId == request.SubjectId &&
                x.Session == currentSession &&
                x.Term == currentTerm.Term &&
                studentIds.Contains(x.StudentId))
            .ToListAsync();

        var existingByStudent = existingResults
            .ToDictionary(x => x.StudentId);

        // ============================================================
        // PROCESS RESULTS
        // ============================================================

        var uploadedResults = new List<object>();

        foreach (var item in request.Results)
        {
            // --------------------------------------------------------
            // EXISTING RESULT
            // --------------------------------------------------------

            if (existingByStudent.TryGetValue(
                    item.StudentId,
                    out var result))
            {
                // Preserve the existing test score.
                var testScore = result.TestScore ?? 0m;

                // Update only exam.
                result.ExamScore = item.ExamScore;

                // Recalculate total.
                result.Score =
                    testScore +
                    item.ExamScore;

                // Recalculate grade.
                result.Grade =
                    CalculateGrade(result.Score);

                // Update remark if supplied.
                result.Remark = item.Remark;

                var student = students
                    .First(x => x.Id == item.StudentId);

                uploadedResults.Add(new
                {
                    resultId = result.Id,

                    studentId = result.StudentId,
                    studentName = student.studentName,
                    studentNumber = student.studentNumber,

                    classId = result.ClassId,

                    subjectId = result.SubjectId,
                    subjectName = subject.Name,
                    subjectCode = subject.Code,

                    testScore = result.TestScore,
                    examScore = result.ExamScore,

                    score = result.Score,
                    grade = result.Grade,

                    remark = result.Remark,

                    session = result.Session,
                    term = result.Term
                });
            }
            else
            {
                // ----------------------------------------------------
                // CREATE NEW RESULT
                // ----------------------------------------------------

                var totalScore =
                    item.ExamScore;

                var grade =
                    CalculateGrade(totalScore);

                var newResult =
                    new Models.Results.StudentResult
                    {
                        Id = Guid.NewGuid(),

                        StudentId = item.StudentId,
                        SchoolId = schoolId,

                        ClassId = request.ClassId,
                        SubjectId = request.SubjectId,

                        Session = currentSession,
                        Term = currentTerm.Term,

                        // Test has not been uploaded yet.
                        TestScore = 0m,

                        ExamScore = item.ExamScore,

                        // Test 0 + Exam
                        Score = totalScore,

                        Grade = grade,

                        Remark = item.Remark,

                        CreatedAt = DateTime.UtcNow
                    };

                _context.StudentResults.Add(newResult);

                var student = students
                    .First(x => x.Id == item.StudentId);

                uploadedResults.Add(new
                {
                    resultId = newResult.Id,

                    studentId = newResult.StudentId,
                    studentName = student.studentName,
                    studentNumber = student.studentNumber,

                    classId = newResult.ClassId,

                    subjectId = newResult.SubjectId,
                    subjectName = subject.Name,
                    subjectCode = subject.Code,

                    testScore = newResult.TestScore,
                    examScore = newResult.ExamScore,

                    score = newResult.Score,
                    grade = newResult.Grade,

                    remark = newResult.Remark,

                    session = newResult.Session,
                    term = newResult.Term
                });
            }
        }

        // ============================================================
        // SAVE
        // ============================================================

        await _context.SaveChangesAsync();

        // ============================================================
        // RESPONSE
        // ============================================================

        return (
            true,
            new
            {
                schoolId,

                classId = request.ClassId,

                subjectId = subject.Id,
                subjectName = subject.Name,
                subjectCode = subject.Code,

                session = currentSession,
                term = currentTerm,

                uploadedCount = uploadedResults.Count,

                results = uploadedResults
            },
            null
        );
    }


    // ================================================================
    // BULK TEST RESULT
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        BulkTestResultAsync(
            string userId,
            BulkTestResultRequest request)
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

        var period = await _context.AcademicSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.SchoolId == schoolId &&
                x.IsCurrent);

        if (period == null)
        {
            return (
                false,
                null,
                "There is no active academic session."
            );
        }

        var currentSession = period.Session;
        var currentTerm = await _context.AcademicTerms
    .AsNoTracking()
    .FirstOrDefaultAsync(x =>
        x.AcademicSessionId == period.Id &&
        x.IsCurrent);

        if (currentTerm == null)
        {
            return (
                false,
                null,
                "There is no active academic term."
            );
        }

        // ============================================================
        // VALIDATE CLASS
        // ============================================================

        var classExists = await _context.TeacherClasses
            .AsNoTracking()
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.ClassId == request.ClassId &&
                x.Class.SchoolId == schoolId);

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
        // TEACHER MUST TEACH SUBJECT
        // ============================================================

        var teachesSubject = await _context.TeacherSubjects
            .AsNoTracking()
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.SubjectId == request.SubjectId &&
                x.Subject.SchoolId == schoolId);

        if (!teachesSubject)
        {
            return (
                false,
                null,
                "You are not assigned to this subject."
            );
        }

        // ============================================================
        // VALIDATE RESULTS
        // ============================================================

        if (request.Results == null ||
            request.Results.Count == 0)
        {
            return (
                false,
                null,
                "At least one student result is required."
            );
        }

        // ============================================================
        // CHECK DUPLICATE STUDENTS IN REQUEST
        // ============================================================

        var duplicateStudentIds = request.Results
            .GroupBy(x => x.StudentId)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (duplicateStudentIds.Count > 0)
        {
            return (
                false,
                null,
                "The same student cannot appear more than once in the upload."
            );
        }

        // ============================================================
        // VALIDATE TEST SCORES
        // ============================================================

        var invalidScore = request.Results
            .FirstOrDefault(x =>
                x.TestScore < 0 ||
                x.TestScore > 40);

        if (invalidScore != null)
        {
            return (
                false,
                null,
                $"Test score for student {invalidScore.StudentId} must be between 0 and 40."
            );
        }

        // ============================================================
        // STUDENT IDS
        // ============================================================

        var studentIds = request.Results
            .Select(x => x.StudentId)
            .ToList();

        // ============================================================
        // GET STUDENTS
        // ============================================================

        var students = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x =>
                studentIds.Contains(x.Id) &&
                x.SchoolId == schoolId &&
                x.ClassId == request.ClassId)
            .Select(x => new
            {
                x.Id,
                x.ClassId,

                studentName = x.User.FullName,
                studentNumber = x.StudentNumber
            })
            .ToListAsync();

        // ============================================================
        // CHECK ALL STUDENTS EXIST IN CLASS
        // ============================================================

        var foundStudentIds = students
            .Select(x => x.Id)
            .ToHashSet();

        var invalidStudents = studentIds
            .Where(x => !foundStudentIds.Contains(x))
            .ToList();

        if (invalidStudents.Count > 0)
        {
            return (
                false,
                null,
                "One or more students were not found in the selected class."
            );
        }

        // ============================================================
        // GET EXISTING RESULTS
        // ============================================================

        var existingResults = await _context.StudentResults
            .Where(x =>
                x.SchoolId == schoolId &&
                x.ClassId == request.ClassId &&
                x.SubjectId == request.SubjectId &&
                x.Session == currentSession &&
                x.Term == currentTerm.Term &&
                studentIds.Contains(x.StudentId))
            .ToListAsync();

        var existingByStudent = existingResults
            .ToDictionary(x => x.StudentId);

        // ============================================================
        // PROCESS RESULTS
        // ============================================================

        var uploadedResults = new List<object>();

        foreach (var item in request.Results)
        {
            // ========================================================
            // EXISTING RESULT
            // ========================================================

            if (existingByStudent.TryGetValue(
                    item.StudentId,
                    out var result))
            {
                // Preserve existing exam score.
                var examScore = result.ExamScore ?? 0m;

                // Update only test.
                result.TestScore = item.TestScore;

                // Recalculate total.
                result.Score =
                    item.TestScore +
                    examScore;

                // Recalculate grade.
                result.Grade =
                    CalculateGrade(result.Score);

                // Update remark.
                result.Remark = item.Remark;

                var student = students
                    .First(x => x.Id == item.StudentId);

                uploadedResults.Add(new
                {
                    resultId = result.Id,

                    studentId = result.StudentId,
                    studentName = student.studentName,
                    studentNumber = student.studentNumber,

                    classId = result.ClassId,

                    subjectId = result.SubjectId,
                    subjectName = subject.Name,
                    subjectCode = subject.Code,

                    testScore = result.TestScore,
                    examScore = result.ExamScore,

                    score = result.Score,
                    grade = result.Grade,

                    remark = result.Remark,

                    session = result.Session,
                    term = result.Term
                });
            }
            else
            {
                // ====================================================
                // CREATE NEW RESULT
                // ====================================================

                var totalScore =
                    item.TestScore;

                var grade =
                    CalculateGrade(totalScore);

                var newResult =
                    new Models.Results.StudentResult
                    {
                        Id = Guid.NewGuid(),

                        StudentId = item.StudentId,
                        SchoolId = schoolId,

                        ClassId = request.ClassId,
                        SubjectId = request.SubjectId,

                        Session = currentSession,
                        Term = currentTerm.Term,

                        // Test uploaded.
                        TestScore = item.TestScore,

                        // Exam has not been uploaded yet.
                        ExamScore = 0m,

                        // Test + Exam
                        Score = totalScore,

                        Grade = grade,

                        Remark = item.Remark,

                        CreatedAt = DateTime.UtcNow
                    };

                _context.StudentResults.Add(newResult);

                var student = students
                    .First(x => x.Id == item.StudentId);

                uploadedResults.Add(new
                {
                    resultId = newResult.Id,

                    studentId = newResult.StudentId,
                    studentName = student.studentName,
                    studentNumber = student.studentNumber,

                    classId = newResult.ClassId,

                    subjectId = newResult.SubjectId,
                    subjectName = subject.Name,
                    subjectCode = subject.Code,

                    testScore = newResult.TestScore,
                    examScore = newResult.ExamScore,

                    score = newResult.Score,
                    grade = newResult.Grade,

                    remark = newResult.Remark,

                    session = newResult.Session,
                    term = newResult.Term
                });
            }
        }

        // ============================================================
        // SAVE
        // ============================================================

        await _context.SaveChangesAsync();

        // ============================================================
        // RESPONSE
        // ============================================================

        return (
            true,
            new
            {
                schoolId,

                classId = request.ClassId,

                subjectId = subject.Id,
                subjectName = subject.Name,
                subjectCode = subject.Code,

                session = currentSession,
                term = currentTerm.Term,

                uploadedCount = uploadedResults.Count,

                results = uploadedResults
            },
            null
        );
    }

    // ================================================================
    // BULK TEST + EXAM RESULT
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        BulkResultAsync(
            string userId,
            BulkResultRequest request)
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

        var period = await _context.AcademicSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.SchoolId == schoolId &&
                x.IsCurrent);

        if (period == null)
        {
            return (
                false,
                null,
                "There is no active academic session."
            );
        }

        var currentSession = period.Session;
        var currentTerm = await _context.AcademicTerms
    .AsNoTracking()
    .FirstOrDefaultAsync(x =>
        x.AcademicSessionId == period.Id &&
        x.IsCurrent);

        if (currentTerm == null)
        {
            return (
                false,
                null,
                "There is no active academic term."
            );
        }

        // ============================================================
        // VALIDATE CLASS
        // ============================================================

        var classExists = await _context.TeacherClasses
            .AsNoTracking()
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.ClassId == request.ClassId &&
                x.Class.SchoolId == schoolId);

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
        // TEACHER MUST TEACH SUBJECT
        // ============================================================

        var teachesSubject = await _context.TeacherSubjects
            .AsNoTracking()
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.SubjectId == request.SubjectId &&
                x.Subject.SchoolId == schoolId);

        if (!teachesSubject)
        {
            return (
                false,
                null,
                "You are not assigned to this subject."
            );
        }

        // ============================================================
        // VALIDATE RESULTS
        // ============================================================

        if (request.Results == null ||
            request.Results.Count == 0)
        {
            return (
                false,
                null,
                "At least one student result is required."
            );
        }

        // ============================================================
        // CHECK DUPLICATE STUDENTS
        // ============================================================

        var duplicateStudentIds = request.Results
            .GroupBy(x => x.StudentId)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (duplicateStudentIds.Count > 0)
        {
            return (
                false,
                null,
                "The same student cannot appear more than once in the upload."
            );
        }

        // ============================================================
        // VALIDATE TEST SCORES
        // ============================================================

        var invalidTestScore = request.Results
            .FirstOrDefault(x =>
                x.TestScore < 0 ||
                x.TestScore > 40);

        if (invalidTestScore != null)
        {
            return (
                false,
                null,
                $"Test score for student {invalidTestScore.StudentId} must be between 0 and 40."
            );
        }

        // ============================================================
        // VALIDATE EXAM SCORES
        // ============================================================

        var invalidExamScore = request.Results
            .FirstOrDefault(x =>
                x.ExamScore < 0 ||
                x.ExamScore > 60);

        if (invalidExamScore != null)
        {
            return (
                false,
                null,
                $"Exam score for student {invalidExamScore.StudentId} must be between 0 and 60."
            );
        }

        // ============================================================
        // STUDENT IDS
        // ============================================================

        var studentIds = request.Results
            .Select(x => x.StudentId)
            .ToList();

        // ============================================================
        // GET STUDENTS
        // ============================================================

        var students = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x =>
                studentIds.Contains(x.Id) &&
                x.SchoolId == schoolId &&
                x.ClassId == request.ClassId)
            .Select(x => new
            {
                x.Id,
                x.ClassId,

                studentName = x.User.FullName,
                studentNumber = x.StudentNumber
            })
            .ToListAsync();

        // ============================================================
        // CHECK ALL STUDENTS EXIST
        // ============================================================

        var foundStudentIds = students
            .Select(x => x.Id)
            .ToHashSet();

        var invalidStudents = studentIds
            .Where(x => !foundStudentIds.Contains(x))
            .ToList();

        if (invalidStudents.Count > 0)
        {
            return (
                false,
                null,
                "One or more students were not found in the selected class."
            );
        }

        // ============================================================
        // GET EXISTING RESULTS
        // ============================================================

        var existingResults = await _context.StudentResults
            .Where(x =>
                x.SchoolId == schoolId &&
                x.ClassId == request.ClassId &&
                x.SubjectId == request.SubjectId &&
                x.Session == currentSession &&
                x.Term == currentTerm.Term &&
                studentIds.Contains(x.StudentId))
            .ToListAsync();

        var existingByStudent = existingResults
            .ToDictionary(x => x.StudentId);

        // ============================================================
        // PROCESS RESULTS
        // ============================================================

        var uploadedResults = new List<object>();

        foreach (var item in request.Results)
        {
            // ========================================================
            // CALCULATE TOTAL
            // ========================================================

            var totalScore =
                item.TestScore +
                item.ExamScore;

            // ========================================================
            // CALCULATE GRADE
            // ========================================================

            var grade =
                CalculateGrade(totalScore);

            // ========================================================
            // EXISTING RESULT
            // ========================================================

            if (existingByStudent.TryGetValue(
                    item.StudentId,
                    out var result))
            {
                result.TestScore = item.TestScore;
                result.ExamScore = item.ExamScore;

                result.Score = totalScore;

                result.Grade = grade;

                result.Remark = item.Remark;

                var student = students
                    .First(x => x.Id == item.StudentId);

                uploadedResults.Add(new
                {
                    resultId = result.Id,

                    studentId = result.StudentId,
                    studentName = student.studentName,
                    studentNumber = student.studentNumber,

                    classId = result.ClassId,

                    subjectId = result.SubjectId,
                    subjectName = subject.Name,
                    subjectCode = subject.Code,

                    testScore = result.TestScore,
                    examScore = result.ExamScore,

                    score = result.Score,
                    grade = result.Grade,

                    remark = result.Remark,

                    session = result.Session,
                    term = result.Term
                });
            }
            else
            {
                // ====================================================
                // CREATE NEW RESULT
                // ====================================================

                var newResult =
                    new Models.Results.StudentResult
                    {
                        Id = Guid.NewGuid(),

                        StudentId = item.StudentId,
                        SchoolId = schoolId,

                        ClassId = request.ClassId,
                        SubjectId = request.SubjectId,

                        Session = currentSession,
                        Term = currentTerm.Term,

                        TestScore = item.TestScore,
                        ExamScore = item.ExamScore,

                        Score = totalScore,

                        Grade = grade,

                        Remark = item.Remark,

                        CreatedAt = DateTime.UtcNow
                    };

                _context.StudentResults.Add(newResult);

                var student = students
                    .First(x => x.Id == item.StudentId);

                uploadedResults.Add(new
                {
                    resultId = newResult.Id,

                    studentId = newResult.StudentId,
                    studentName = student.studentName,
                    studentNumber = student.studentNumber,

                    classId = newResult.ClassId,

                    subjectId = newResult.SubjectId,
                    subjectName = subject.Name,
                    subjectCode = subject.Code,

                    testScore = newResult.TestScore,
                    examScore = newResult.ExamScore,

                    score = newResult.Score,
                    grade = newResult.Grade,

                    remark = newResult.Remark,

                    session = newResult.Session,
                    term = newResult.Term
                });
            }
        }

        // ============================================================
        // SAVE
        // ============================================================

        await _context.SaveChangesAsync();

        // ============================================================
        // RESPONSE
        // ============================================================

        return (
            true,
            new
            {
                schoolId,

                classId = request.ClassId,

                subjectId = subject.Id,
                subjectName = subject.Name,
                subjectCode = subject.Code,

                session = currentSession,
                term = currentTerm,

                uploadedCount = uploadedResults.Count,

                results = uploadedResults
            },
            null
        );
    }

    // ================================================================
    // UPLOAD TEST RESULT
    // TEST = 40
    // EXAM = 60
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        UploadTestResultAsync(
            string userId,
            UploadTestResultRequest request)
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

        var period = await _context.AcademicSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.SchoolId == schoolId &&
                x.IsCurrent);

        if (period == null)
        {
            return (
                false,
                null,
                "There is no active academic session."
            );
        }

        var currentSession = period.Session;
        var currentTerm = await _context.AcademicTerms
    .AsNoTracking()
    .FirstOrDefaultAsync(x =>
        x.AcademicSessionId == period.Id &&
        x.IsCurrent);

        if (currentTerm == null)
        {
            return (
                false,
                null,
                "There is no active academic term."
            );
        }

        // ============================================================
        // VALIDATE TEST SCORE
        // ============================================================

        if (request.TestScore < 0 ||
            request.TestScore > 40)
        {
            return (
                false,
                null,
                "Test score must be between 0 and 40."
            );
        }

        // ============================================================
        // VALIDATE STUDENT
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
        // TEACHER MUST TEACH CLASS
        // ============================================================

        var teachesClass = await _context.TeacherClasses
            .AsNoTracking()
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.ClassId == request.ClassId &&
                x.Class.SchoolId == schoolId);

        if (!teachesClass)
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
        // TEACHER MUST TEACH SUBJECT
        // ============================================================

        var teachesSubject = await _context.TeacherSubjects
            .AsNoTracking()
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.SubjectId == request.SubjectId &&
                x.Subject.SchoolId == schoolId);

        if (!teachesSubject)
        {
            return (
                false,
                null,
                "You are not assigned to this subject."
            );
        }

        // ============================================================
        // FIND EXISTING RESULT
        // ============================================================

        var result = await _context.StudentResults
            .FirstOrDefaultAsync(x =>
                x.StudentId == request.StudentId &&
                x.SchoolId == schoolId &&
                x.ClassId == request.ClassId &&
                x.SubjectId == request.SubjectId &&
                x.Session == currentSession &&
                x.Term == currentTerm.Term);

        // ============================================================
        // UPDATE EXISTING RESULT
        // ============================================================

        if (result != null)
        {
            // Preserve existing exam score.
            var examScore = result.ExamScore ?? 0m;

            result.TestScore = request.TestScore;

            // Test + existing exam
            result.Score =
                request.TestScore +
                examScore;

            // Recalculate grade
            result.Grade =
                CalculateGrade(result.Score);

            result.Remark = request.Remark;

            await _context.SaveChangesAsync();

            return (
                true,
                new
                {
                    resultId = result.Id,

                    schoolId,

                    studentId = student.Id,
                    studentName = student.studentName,
                    studentNumber = student.studentNumber,

                    classId = result.ClassId,
                    className = student.className,

                    subjectId = subject.Id,
                    subjectName = subject.Name,
                    subjectCode = subject.Code,

                    session = result.Session,
                    term = result.Term,

                    testScore = result.TestScore,
                    examScore = result.ExamScore,

                    score = result.Score,
                    grade = result.Grade,

                    remark = result.Remark,

                    createdAt = result.CreatedAt
                },
                null
            );
        }

        // ============================================================
        // CREATE NEW RESULT
        // ============================================================

        var newResult = new Models.Results.StudentResult
        {
            Id = Guid.NewGuid(),

            StudentId = student.Id,
            SchoolId = schoolId,

            ClassId = request.ClassId,
            SubjectId = request.SubjectId,

            Session = currentSession,
            Term = currentTerm.Term,

            // Test uploaded
            TestScore = request.TestScore,

            // Exam not uploaded yet
            ExamScore = 0m,

            // Test + Exam
            Score = request.TestScore,

            Grade = CalculateGrade(request.TestScore),

            Remark = request.Remark,

            CreatedAt = DateTime.UtcNow
        };

        _context.StudentResults.Add(newResult);

        await _context.SaveChangesAsync();

        // ============================================================
        // RESPONSE
        // ============================================================

        return (
            true,
            new
            {
                resultId = newResult.Id,

                schoolId,

                studentId = student.Id,
                studentName = student.studentName,
                studentNumber = student.studentNumber,

                classId = newResult.ClassId,
                className = student.className,

                subjectId = subject.Id,
                subjectName = subject.Name,
                subjectCode = subject.Code,

                session = newResult.Session,
                term = newResult.Term,

                testScore = newResult.TestScore,
                examScore = newResult.ExamScore,

                score = newResult.Score,
                grade = newResult.Grade,

                remark = newResult.Remark,

                createdAt = newResult.CreatedAt
            },
            null
        );
    }

    // ================================================================
    // UPLOAD EXAM RESULT
    // TEST = 40
    // EXAM = 60
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        UploadExamResultAsync(
            string userId,
            UploadExamResultRequest request)
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

        var period = await _context.AcademicSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.SchoolId == schoolId &&
                x.IsCurrent);

        if (period == null)
        {
            return (
                false,
                null,
                "There is no active academic session."
            );
        }

        var currentSession = period.Session;
        var currentTerm = await _context.AcademicTerms
    .AsNoTracking()
    .FirstOrDefaultAsync(x =>
        x.AcademicSessionId == period.Id &&
        x.IsCurrent);

        if (currentTerm == null)
        {
            return (
                false,
                null,
                "There is no active academic term."
            );
        }

        // ============================================================
        // VALIDATE EXAM SCORE
        // ============================================================

        if (request.ExamScore < 0 ||
            request.ExamScore > 60)
        {
            return (
                false,
                null,
                "Exam score must be between 0 and 60."
            );
        }

        // ============================================================
        // VALIDATE STUDENT
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
        // TEACHER MUST TEACH CLASS
        // ============================================================

        var teachesClass = await _context.TeacherClasses
            .AsNoTracking()
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.ClassId == request.ClassId &&
                x.Class.SchoolId == schoolId);

        if (!teachesClass)
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
        // TEACHER MUST TEACH SUBJECT
        // ============================================================

        var teachesSubject = await _context.TeacherSubjects
            .AsNoTracking()
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.SubjectId == request.SubjectId &&
                x.Subject.SchoolId == schoolId);

        if (!teachesSubject)
        {
            return (
                false,
                null,
                "You are not assigned to this subject."
            );
        }

        // ============================================================
        // FIND EXISTING RESULT
        // ============================================================

        var result = await _context.StudentResults
            .FirstOrDefaultAsync(x =>
                x.StudentId == request.StudentId &&
                x.SchoolId == schoolId &&
                x.ClassId == request.ClassId &&
                x.SubjectId == request.SubjectId &&
                x.Session == currentSession &&
                x.Term == currentTerm.Term);

        // ============================================================
        // UPDATE EXISTING RESULT
        // ============================================================

        if (result != null)
        {
            // Preserve existing test score.
            var testScore = result.TestScore ?? 0m;

            result.ExamScore = request.ExamScore;

            // Existing test + new exam
            result.Score =
                testScore +
                request.ExamScore;

            // Recalculate grade
            result.Grade =
                CalculateGrade(result.Score);

            result.Remark = request.Remark;

            await _context.SaveChangesAsync();

            return (
                true,
                new
                {
                    resultId = result.Id,

                    schoolId,

                    studentId = student.Id,
                    studentName = student.studentName,
                    studentNumber = student.studentNumber,

                    classId = result.ClassId,
                    className = student.className,

                    subjectId = subject.Id,
                    subjectName = subject.Name,
                    subjectCode = subject.Code,

                    session = result.Session,
                    term = result.Term,

                    testScore = result.TestScore,
                    examScore = result.ExamScore,

                    score = result.Score,
                    grade = result.Grade,

                    remark = result.Remark,

                    createdAt = result.CreatedAt
                },
                null
            );
        }

        // ============================================================
        // CREATE NEW RESULT
        // ============================================================

        var newResult = new Models.Results.StudentResult
        {
            Id = Guid.NewGuid(),

            StudentId = student.Id,
            SchoolId = schoolId,

            ClassId = request.ClassId,
            SubjectId = request.SubjectId,

            Session = currentSession,
            Term = currentTerm.Term,

            // Test not uploaded yet
            TestScore = 0m,

            // Exam uploaded
            ExamScore = request.ExamScore,

            // Test + Exam
            Score = request.ExamScore,

            Grade = CalculateGrade(request.ExamScore),

            Remark = request.Remark,

            CreatedAt = DateTime.UtcNow
        };

        _context.StudentResults.Add(newResult);

        await _context.SaveChangesAsync();

        // ============================================================
        // RESPONSE
        // ============================================================

        return (
            true,
            new
            {
                resultId = newResult.Id,

                schoolId,

                studentId = student.Id,
                studentName = student.studentName,
                studentNumber = student.studentNumber,

                classId = newResult.ClassId,
                className = student.className,

                subjectId = subject.Id,
                subjectName = subject.Name,
                subjectCode = subject.Code,

                session = newResult.Session,
                term = newResult.Term,

                testScore = newResult.TestScore,
                examScore = newResult.ExamScore,

                score = newResult.Score,
                grade = newResult.Grade,

                remark = newResult.Remark,

                createdAt = newResult.CreatedAt
            },
            null
        );
    }

    // ================================================================
    // UPLOAD COMPLETE RESULT
    // TEST = 40
    // EXAM = 60
    // TOTAL = 100
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        UploadResultAsync(
            string userId,
            UploadResultRequest request)
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

        var period = await _context.AcademicSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.SchoolId == schoolId &&
                x.IsCurrent);

        if (period == null)
        {
            return (
                false,
                null,
                "There is no active academic session."
            );
        }

        var currentSession = period.Session;
        var currentTerm = await _context.AcademicTerms
    .AsNoTracking()
    .FirstOrDefaultAsync(x =>
        x.AcademicSessionId == period.Id &&
        x.IsCurrent);

        if (currentTerm == null)
        {
            return (
                false,
                null,
                "There is no active academic term."
            );
        }

        // ============================================================
        // VALIDATE TEST SCORE
        // ============================================================

        if (request.TestScore < 0 ||
            request.TestScore > 40)
        {
            return (
                false,
                null,
                "Test score must be between 0 and 40."
            );
        }

        // ============================================================
        // VALIDATE EXAM SCORE
        // ============================================================

        if (request.ExamScore < 0 ||
            request.ExamScore > 60)
        {
            return (
                false,
                null,
                "Exam score must be between 0 and 60."
            );
        }

        // ============================================================
        // VALIDATE STUDENT
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
        // TEACHER MUST TEACH CLASS
        // ============================================================

        var teachesClass = await _context.TeacherClasses
            .AsNoTracking()
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.ClassId == request.ClassId &&
                x.Class.SchoolId == schoolId);

        if (!teachesClass)
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
        // TEACHER MUST TEACH SUBJECT
        // ============================================================

        var teachesSubject = await _context.TeacherSubjects
            .AsNoTracking()
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.SubjectId == request.SubjectId &&
                x.Subject.SchoolId == schoolId);

        if (!teachesSubject)
        {
            return (
                false,
                null,
                "You are not assigned to this subject."
            );
        }

        // ============================================================
        // CHECK EXISTING RESULT
        // ============================================================

        var result = await _context.StudentResults
            .FirstOrDefaultAsync(x =>
                x.StudentId == request.StudentId &&
                x.SchoolId == schoolId &&
                x.ClassId == request.ClassId &&
                x.SubjectId == request.SubjectId &&
                x.Session == currentSession &&
                x.Term == currentTerm.Term);

        // ============================================================
        // CALCULATE TOTAL
        // ============================================================

        var totalScore =
            request.TestScore +
            request.ExamScore;

        // ============================================================
        // CALCULATE GRADE
        // ============================================================

        var grade =
            CalculateGrade(totalScore);

        // ============================================================
        // UPDATE EXISTING RESULT
        // ============================================================

        if (result != null)
        {
            result.TestScore = request.TestScore;
            result.ExamScore = request.ExamScore;

            result.Score = totalScore;

            result.Grade = grade;

            result.Remark = request.Remark;

            await _context.SaveChangesAsync();

            return (
                true,
                new
                {
                    resultId = result.Id,

                    schoolId,

                    studentId = student.Id,
                    studentName = student.studentName,
                    studentNumber = student.studentNumber,

                    classId = result.ClassId,
                    className = student.className,

                    subjectId = subject.Id,
                    subjectName = subject.Name,
                    subjectCode = subject.Code,

                    session = result.Session,
                    term = result.Term,

                    testScore = result.TestScore,
                    examScore = result.ExamScore,

                    score = result.Score,
                    grade = result.Grade,

                    remark = result.Remark,

                    createdAt = result.CreatedAt
                },
                null
            );
        }

        // ============================================================
        // CREATE NEW RESULT
        // ============================================================

        var newResult = new Models.Results.StudentResult
        {
            Id = Guid.NewGuid(),

            StudentId = student.Id,
            SchoolId = schoolId,

            ClassId = request.ClassId,
            SubjectId = subject.Id,

            Session = currentSession,
            Term = currentTerm.Term,

            TestScore = request.TestScore,
            ExamScore = request.ExamScore,

            Score = totalScore,

            Grade = grade,

            Remark = request.Remark,

            CreatedAt = DateTime.UtcNow
        };

        _context.StudentResults.Add(newResult);

        await _context.SaveChangesAsync();

        // ============================================================
        // RESPONSE
        // ============================================================

        return (
            true,
            new
            {
                resultId = newResult.Id,

                schoolId,

                studentId = student.Id,
                studentName = student.studentName,
                studentNumber = student.studentNumber,

                classId = newResult.ClassId,
                className = student.className,

                subjectId = subject.Id,
                subjectName = subject.Name,
                subjectCode = subject.Code,

                session = newResult.Session,
                term = newResult.Term,

                testScore = newResult.TestScore,
                examScore = newResult.ExamScore,

                score = newResult.Score,
                grade = newResult.Grade,

                remark = newResult.Remark,

                createdAt = newResult.CreatedAt
            },
            null
        );
    }

    public async Task<(bool Success, object? Data, string? Error)>
        UpdateTestResultAsync(
            string userId,
            Guid resultId,
            UpdateTestResultRequest request)
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
        // CURRENT ACADEMIC SESSION
        // ============================================================

        var period = await _context.AcademicSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.SchoolId == schoolId &&
                x.IsCurrent);

        if (period == null)
        {
            return (
                false,
                null,
                "There is no active academic session."
            );
        }

        var currentTerm = await _context.AcademicTerms
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.AcademicSessionId == period.Id &&
                x.IsCurrent);

        if (currentTerm == null)
        {
            return (
                false,
                null,
                "There is no active academic term."
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
                null,
                "Result not found."
            );
        }

        // ============================================================
        // RESULT MUST BE CURRENT
        // ============================================================

        if (result.Session != period.Session ||
            result.Term != currentTerm.Term)
        {
            return (
                false,
                null,
                "Only results for the current academic period can be edited."
            );
        }


        // ============================================================
        // TEACHER MUST TEACH CLASS
        // ============================================================

        var teachesClass = await _context.TeacherClasses
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.ClassId == result.ClassId &&
                x.Class.SchoolId == schoolId);

        if (!teachesClass)
        {
            return (
                false,
                null,
                "You are not assigned to this class."
            );
        }

        // ============================================================
        // TEACHER MUST TEACH SUBJECT
        // ============================================================

        var teachesSubject = await _context.TeacherSubjects
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.SubjectId == result.SubjectId &&
                x.Subject.SchoolId == schoolId);

        if (!teachesSubject)
        {
            return (
                false,
                null,
                "You are not assigned to this subject."
            );
        }

        // ============================================================
        // VALIDATE TEST
        // ============================================================

        if (request.TestScore < 0 ||
            request.TestScore > 40)
        {
            return (
                false,
                null,
                "Test score must be between 0 and 40."
            );
        }

        // ============================================================
        // UPDATE TEST ONLY
        // ============================================================

        result.TestScore = request.TestScore;

        // Preserve existing exam score.
        var examScore = result.ExamScore ?? 0m;

        // Recalculate total.
        result.Score =
            request.TestScore +
            examScore;

        // Recalculate grade.
        result.Grade =
            CalculateGrade(result.Score);

        // Update remark if provided.
        result.Remark = request.Remark;

        await _context.SaveChangesAsync();

        // ============================================================
        // RESPONSE
        // ============================================================

        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x =>
                x.Id == result.StudentId &&
                x.SchoolId == schoolId)
            .Select(x => new
            {
                x.Id,
                studentName = x.User.FullName,
                studentNumber = x.StudentNumber,
                className = x.Class.Name
            })
            .FirstOrDefaultAsync();

        var subject = await _context.Subjects
            .AsNoTracking()
            .Where(x =>
                x.Id == result.SubjectId &&
                x.SchoolId == schoolId)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Code
            })
            .FirstOrDefaultAsync();

        return (
            true,
            new
            {
                resultId = result.Id,

                schoolId,

                studentId = result.StudentId,
                studentName = student?.studentName,
                studentNumber = student?.studentNumber,

                classId = result.ClassId,
                className = student?.className,

                subjectId = result.SubjectId,
                subjectName = subject?.Name,
                subjectCode = subject?.Code,

                session = result.Session,
                term = result.Term,

                testScore = result.TestScore,
                examScore = result.ExamScore,

                score = result.Score,
                grade = result.Grade,

                remark = result.Remark,

                updatedAt = DateTime.UtcNow
            },
            null
        );
    }
    public async Task<(bool Success, object? Data, string? Error)>
    UpdateExamResultAsync(
    string userId,
    Guid resultId,
    UpdateExamResultRequest request)
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
        // CURRENT ACADEMIC SESSION
        // ============================================================

        var period = await _context.AcademicSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.SchoolId == schoolId &&
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
        // CURRENT ACADEMIC TERM
        // ============================================================

        var currentTerm = await _context.AcademicTerms
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.AcademicSessionId == period.Id &&
                x.IsCurrent);

        if (currentTerm == null)
        {
            return (
                false,
                null,
                "There is no active academic term."
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
                null,
                "Result not found."
            );
        }

        // ============================================================
        // RESULT MUST BELONG TO CURRENT SESSION AND TERM
        // ============================================================

        if (result.Session != period.Session ||
            result.Term != currentTerm.Term)
        {
            return (
                false,
                null,
                "Only results for the current academic period can be edited."
            );
        }

        // ============================================================
        // TEACHER MUST TEACH CLASS
        // ============================================================

        var teachesClass = await _context.TeacherClasses
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.ClassId == result.ClassId &&
                x.Class.SchoolId == schoolId);

        if (!teachesClass)
        {
            return (
                false,
                null,
                "You are not assigned to this class."
            );
        }

        // ============================================================
        // TEACHER MUST TEACH SUBJECT
        // ============================================================

        var teachesSubject = await _context.TeacherSubjects
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.SubjectId == result.SubjectId &&
                x.Subject.SchoolId == schoolId);

        if (!teachesSubject)
        {
            return (
                false,
                null,
                "You are not assigned to this subject."
            );
        }

        // ============================================================
        // VALIDATE EXAM
        // ============================================================

        if (request.ExamScore < 0 ||
            request.ExamScore > 60)
        {
            return (
                false,
                null,
                "Exam score must be between 0 and 60."
            );
        }

        // ============================================================
        // UPDATE EXAM ONLY
        // ============================================================

        result.ExamScore = request.ExamScore;

        // Preserve existing test score.
        var testScore = result.TestScore ?? 0m;

        // Recalculate total.
        result.Score =
            testScore +
            request.ExamScore;

        // Recalculate grade.
        result.Grade =
            CalculateGrade(result.Score);

        // Update remark.
        result.Remark = request.Remark;

        await _context.SaveChangesAsync();

        // ============================================================
        // GET RESPONSE DATA
        // ============================================================

        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x =>
                x.Id == result.StudentId &&
                x.SchoolId == schoolId)
            .Select(x => new
            {
                x.Id,
                studentName = x.User.FullName,
                studentNumber = x.StudentNumber,
                className = x.Class.Name
            })
            .FirstOrDefaultAsync();

        var subject = await _context.Subjects
            .AsNoTracking()
            .Where(x =>
                x.Id == result.SubjectId &&
                x.SchoolId == schoolId)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Code
            })
            .FirstOrDefaultAsync();

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
                studentName = student?.studentName,
                studentNumber = student?.studentNumber,

                classId = result.ClassId,
                className = student?.className,

                subjectId = result.SubjectId,
                subjectName = subject?.Name,
                subjectCode = subject?.Code,

                session = result.Session,
                term = result.Term,

                testScore = result.TestScore,
                examScore = result.ExamScore,

                score = result.Score,
                grade = result.Grade,

                remark = result.Remark,

                updatedAt = DateTime.UtcNow
            },
            null
        );
    }

    public async Task<(bool Success, object? Data, string? Error)>
        UpdateResultAsync(
            string userId,
            Guid resultId,
            UpdateResultRequest request)
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
        // CURRENT ACADEMIC SESSION
        // ============================================================

        var period = await _context.AcademicSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.SchoolId == schoolId &&
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
        // CURRENT ACADEMIC TERM
        // ============================================================

        var currentTerm = await _context.AcademicTerms
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.AcademicSessionId == period.Id &&
                x.IsCurrent);

        if (currentTerm == null)
        {
            return (
                false,
                null,
                "There is no active academic term."
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
                null,
                "Result not found."
            );
        }

        // ============================================================
        // CURRENT SESSION AND TERM ONLY
        // ============================================================

        if (result.Session != period.Session ||
            result.Term != currentTerm.Term)
        {
            return (
                false,
                null,
                "Only results for the current academic period can be edited."
            );
        }

        // ============================================================
        // TEACHER MUST TEACH CLASS
        // ============================================================

        var teachesClass = await _context.TeacherClasses
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.ClassId == result.ClassId &&
                x.Class.SchoolId == schoolId);

        if (!teachesClass)
        {
            return (
                false,
                null,
                "You are not assigned to this class."
            );
        }

        // ============================================================
        // TEACHER MUST TEACH SUBJECT
        // ============================================================

        var teachesSubject = await _context.TeacherSubjects
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.SubjectId == result.SubjectId &&
                x.Subject.SchoolId == schoolId);

        if (!teachesSubject)
        {
            return (
                false,
                null,
                "You are not assigned to this subject."
            );
        }

        // ============================================================
        // VALIDATE TEST
        // ============================================================

        if (request.TestScore < 0 ||
            request.TestScore > 40)
        {
            return (
                false,
                null,
                "Test score must be between 0 and 40."
            );
        }

        // ============================================================
        // VALIDATE EXAM
        // ============================================================

        if (request.ExamScore < 0 ||
            request.ExamScore > 60)
        {
            return (
                false,
                null,
                "Exam score must be between 0 and 60."
            );
        }

        // ============================================================
        // UPDATE BOTH
        // ============================================================

        result.TestScore = request.TestScore;
        result.ExamScore = request.ExamScore;

        result.Score =
            request.TestScore +
            request.ExamScore;

        result.Grade =
            CalculateGrade(result.Score);

        result.Remark = request.Remark;

        await _context.SaveChangesAsync();

        // ============================================================
        // RESPONSE DATA
        // ============================================================

        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x =>
                x.Id == result.StudentId &&
                x.SchoolId == schoolId)
            .Select(x => new
            {
                x.Id,
                studentName = x.User.FullName,
                studentNumber = x.StudentNumber,
                className = x.Class.Name
            })
            .FirstOrDefaultAsync();

        var subject = await _context.Subjects
            .AsNoTracking()
            .Where(x =>
                x.Id == result.SubjectId &&
                x.SchoolId == schoolId)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Code
            })
            .FirstOrDefaultAsync();

        return (
            true,
            new
            {
                resultId = result.Id,

                schoolId,

                studentId = result.StudentId,
                studentName = student?.studentName,
                studentNumber = student?.studentNumber,

                classId = result.ClassId,
                className = student?.className,

                subjectId = result.SubjectId,
                subjectName = subject?.Name,
                subjectCode = subject?.Code,

                session = result.Session,
                term = result.Term,

                testScore = result.TestScore,
                examScore = result.ExamScore,

                score = result.Score,
                grade = result.Grade,

                remark = result.Remark,

                updatedAt = DateTime.UtcNow
            },
            null
        );
    }

    // ================================================================
    // GET TEACHER RESULTS
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetResultsAsync(
            string userId,
            GetTeacherResultsRequest request)
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
        // CURRENT ACADEMIC SESSION
        // ============================================================

        var period = await _context.AcademicSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.SchoolId == schoolId &&
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
        // CURRENT ACADEMIC TERM
        // ============================================================

        var currentTerm = await _context.AcademicTerms
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.AcademicSessionId == period.Id &&
                x.IsCurrent);

        if (currentTerm == null)
        {
            return (
                false,
                null,
                "There is no active academic term."
            );
        }

        // ============================================================
        // BASE QUERY
        // ============================================================

        var query = _context.StudentResults
            .AsNoTracking()
            .Where(x =>
                x.SchoolId == schoolId);

        // ============================================================
        // FILTER BY CLASS
        // ============================================================

        if (request.ClassId.HasValue)
        {
            var teachesClass = await _context.TeacherClasses
                .AsNoTracking()
                .AnyAsync(x =>
                    x.TeacherId == teacher.Id &&
                    x.ClassId == request.ClassId.Value &&
                    x.Class.SchoolId == schoolId);

            if (!teachesClass)
            {
                return (
                    false,
                    null,
                    "You are not assigned to this class."
                );
            }

            query = query.Where(x =>
                x.ClassId == request.ClassId.Value);
        }

        // ============================================================
        // FILTER BY SUBJECT
        // ============================================================

        if (request.SubjectId.HasValue)
        {
            var teachesSubject = await _context.TeacherSubjects
                .AsNoTracking()
                .AnyAsync(x =>
                    x.TeacherId == teacher.Id &&
                    x.SubjectId == request.SubjectId.Value &&
                    x.Subject.SchoolId == schoolId);

            if (!teachesSubject)
            {
                return (
                    false,
                    null,
                    "You are not assigned to this subject."
                );
            }

            query = query.Where(x =>
                x.SubjectId == request.SubjectId.Value);
        }

        // ============================================================
        // SESSION
        // ============================================================

        if (!string.IsNullOrWhiteSpace(request.Session))
        {
            query = query.Where(x =>
                x.Session == request.Session);
        }

        // ============================================================
        // TERM
        // ============================================================

        if (!string.IsNullOrWhiteSpace(request.Term))
        {
            query = query.Where(x =>
                x.Term == request.Term);
        }

        // ============================================================
        // GET RESULTS
        // ============================================================

        var results = await query
            .OrderBy(x => x.Student.User.FullName)
            .Select(x => new
            {
                resultId = x.Id,

                schoolId = x.SchoolId,

                studentId = x.StudentId,
                studentName = x.Student.User.FullName,
                studentNumber = x.Student.StudentNumber,

                classId = x.ClassId,
                className = x.Class.Name,

                subjectId = x.SubjectId,
                subjectName = x.Subject.Name,
                subjectCode = x.Subject.Code,

                session = x.Session,
                term = x.Term,

                testScore = x.TestScore,
                examScore = x.ExamScore,

                score = x.Score,
                grade = x.Grade,

                remark = x.Remark,

                createdAt = x.CreatedAt
            })
            .ToListAsync();

        // ============================================================
        // RESPONSE
        // ============================================================

        return (
            true,
            new
            {
                schoolId,

                currentSession = period.Session,
                currentTerm = currentTerm.Term,

                count = results.Count,

                results
            },
            null
        );
    }

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

        var schoolId = teacher.SchoolId;

        // ============================================================
        // CURRENT ACADEMIC SESSION + CURRENT TERM
        // ============================================================

        var period = await _context.AcademicSessions
            .AsNoTracking()
            .Where(x =>
                x.SchoolId == schoolId &&
                x.IsCurrent)
            .Select(x => new
            {
                x.Session,

                CurrentTerm = x.Terms
                    .Where(t => t.IsCurrent)
                    .Select(t => t.Term)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (period == null)
        {
            return (
                false,
                null,
                "There is no active academic session."
            );
        }

        if (string.IsNullOrWhiteSpace(period.CurrentTerm))
        {
            return (
                false,
                null,
                "There is no active academic term."
            );
        }

        // ============================================================
        // VALIDATE CLASS
        // ============================================================

        var classExists = await _context.TeacherClasses
            .AsNoTracking()
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.ClassId == request.ClassId &&
                x.Class.SchoolId == schoolId);

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
            .AsNoTracking()
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.SubjectId == request.SubjectId &&
                x.Subject.SchoolId == schoolId);

        if (!subjectExists)
        {
            return (
                false,
                null,
                "You are not assigned to this subject."
            );
        }

        // ============================================================
        // VALIDATE TITLE
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

        if (!string.IsNullOrWhiteSpace(request.DueDate))
        {
            if (!DateTime.TryParse(
                request.DueDate,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal |
                DateTimeStyles.AdjustToUniversal,
                out var parsedDate))
            {
                return (
                    false,
                    null,
                    "Invalid due date. Example: 20 August 2026 8:00 AM"
                );
            }

            dueDateUtc = parsedDate;
        }

        // ============================================================
        // CREATE ASSIGNMENT
        // ============================================================

        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),

            SchoolId = schoolId,
            TeacherId = teacher.Id,

            ClassId = request.ClassId,
            SubjectId = request.SubjectId,

            Title = request.Title.Trim(),
            Description = request.Description,
            AttachmentUrl = request.AttachmentUrl,

            Session = period.Session,
            Term = period.CurrentTerm,

            AssignedAt = DateTime.UtcNow,
            DueDate = dueDateUtc,

            IsPublished = true,

            CreatedAt = DateTime.UtcNow
        };

        _context.Assignments.Add(assignment);

        await _context.SaveChangesAsync();

        // ============================================================
        // RESPONSE
        // ============================================================

        return (
            true,
            new
            {
                assignmentId = assignment.Id,

                schoolId = assignment.SchoolId,
                teacherId = assignment.TeacherId,

                classId = assignment.ClassId,
                subjectId = assignment.SubjectId,

                title = assignment.Title,
                description = assignment.Description,
                attachmentUrl = assignment.AttachmentUrl,

                session = assignment.Session,
                term = assignment.Term,

                assignedAt = assignment.AssignedAt,
                dueDate = assignment.DueDate,

                isPublished = assignment.IsPublished
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
        // CURRENT ACADEMIC SESSION
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
        // CURRENT ACADEMIC TERM
        // ============================================================

        var currentTerm = await _context.AcademicTerms
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.AcademicSessionId == period.Id &&
                x.IsCurrent);

        if (currentTerm == null)
        {
            return (
                false,
                null,
                "There is no active academic term."
            );
        }

        // ============================================================
        // CURRENT PERIOD ONLY
        // ============================================================

        if (assignment.Session != period.Session ||
            assignment.Term != currentTerm.Term)
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

        // ============================================================
        // CURRENT ACADEMIC SESSION
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
                "There is no active academic session."
            );
        }

        // ============================================================
        // CURRENT ACADEMIC TERM
        // ============================================================

        var currentTerm = await _context.AcademicTerms
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.AcademicSessionId == period.Id &&
                x.IsCurrent);

        if (currentTerm == null)
        {
            return (
                false,
                "There is no active academic term."
            );
        }

        // ============================================================
        // CURRENT PERIOD ONLY
        // ============================================================

        if (assignment.Session != period.Session ||
            assignment.Term != currentTerm.Term)
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

        var schoolId = teacher.SchoolId;

        // ============================================================
        // CURRENT ACADEMIC SESSION
        // ============================================================

        var period = await _context.AcademicSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.SchoolId == schoolId &&
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
        // CURRENT ACADEMIC TERM
        // ============================================================

        var currentTerm = await _context.AcademicTerms
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.AcademicSessionId == period.Id &&
                x.IsCurrent);

        if (currentTerm == null)
        {
            return (
                false,
                null,
                "There is no active academic term."
            );
        }

        // ============================================================
        // TEACHER MUST TEACH CLASS
        // ============================================================

        var teachesClass = await _context.TeacherClasses
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.ClassId == request.ClassId &&
                x.Class.SchoolId == schoolId);

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
                x.SchoolId == schoolId)
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
                        x.Subject.SchoolId == schoolId);

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
                x.Term == currentTerm.Term);

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

            SchoolId = schoolId,

            StudentId = student.Id,
            ClassId = request.ClassId,

            TeacherId = teacher.Id,

            SubjectId = request.SubjectId,

            AttendanceDate = date,

            Status = request.Status,

            Remarks = request.Remarks,

            Session = period.Session,
            Term = currentTerm.Term,

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

        // ============================================================
        // GET ATTENDANCE RECORD
        // ============================================================

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

        // ============================================================
        // CURRENT ACADEMIC SESSION + TERM
        // ============================================================

        var period = await _context.AcademicSessions
            .AsNoTracking()
            .Where(x =>
                x.SchoolId == teacher.SchoolId &&
                x.IsCurrent)
            .Select(x => new
            {
                x.Session,

                CurrentTerm = x.Terms
                    .Where(t => t.IsCurrent)
                    .Select(t => t.Term)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (period == null)
        {
            return (
                false,
                null,
                "There is no active academic session."
            );
        }

        if (string.IsNullOrWhiteSpace(period.CurrentTerm))
        {
            return (
                false,
                null,
                "There is no active academic term."
            );
        }

        // ============================================================
        // CURRENT PERIOD ONLY
        // ============================================================

        if (record.Session != period.Session ||
            record.Term != period.CurrentTerm)
        {
            return (
                false,
                null,
                "Only attendance from the current academic period can be edited."
            );
        }

        // ============================================================
        // UPDATE
        // ============================================================

        record.Status = request.Status;
        record.Remarks = request.Remarks;
        record.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // ============================================================
        // RESPONSE
        // ============================================================

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

                session = record.Session,
                term = record.Term,

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

        // ============================================================
        // GET ATTENDANCE RECORD
        // ============================================================

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

        // ============================================================
        // CURRENT ACADEMIC SESSION + TERM
        // ============================================================

        var period = await _context.AcademicSessions
            .AsNoTracking()
            .Where(x =>
                x.SchoolId == teacher.SchoolId &&
                x.IsCurrent)
            .Select(x => new
            {
                x.Session,

                CurrentTerm = x.Terms
                    .Where(t => t.IsCurrent)
                    .Select(t => t.Term)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (period == null)
        {
            return (
                false,
                "There is no active academic session."
            );
        }

        if (string.IsNullOrWhiteSpace(period.CurrentTerm))
        {
            return (
                false,
                "There is no active academic term."
            );
        }

        // ============================================================
        // CURRENT PERIOD ONLY
        // ============================================================

        if (record.Session != period.Session ||
            record.Term != period.CurrentTerm)
        {
            return (
                false,
                "Only attendance from the current academic period can be deleted."
            );
        }

        // ============================================================
        // DELETE
        // ============================================================

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