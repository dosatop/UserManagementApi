using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Results;
using UserManagementApi.Models;
using UserManagementApi.Models.Results;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

public class ResultService(
    ApplicationDbContext context,
    IResultGradingService gradingService,
    UserManager<User> userManager
        ) : IResultService
{
    private readonly ApplicationDbContext _context = context;

    private readonly UserManager<User> _userManager = userManager;

    private readonly IResultGradingService _gradingService = gradingService;

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


    // ========================================================================
    // ROLE HELPERS
    // ========================================================================

    private async Task<(bool IsSuperAdmin, bool IsAdmin, bool IsTeacher, bool IsStudent, bool IsParent)>
        GetUserRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return (false, false, false, false, false);
        }

        var roles = await _userManager.GetRolesAsync(user);

        return (
            roles.Contains("SuperAdmin"),
            roles.Contains("Admin"),
            roles.Contains("Teacher"),
            roles.Contains("Student"),
            roles.Contains("Parent")
        );
    }

    // ========================================================================
    // GET USER SCHOOL
    // ========================================================================

    private async Task<(bool Success, Guid? SchoolId, string? Error)>
        GetUserSchoolAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return (
                false,
                null,
                "User not found."
            );
        }

        // ------------------------------------------------------------
        // ADMIN / SUPER ADMIN / GENERAL USER
        // ------------------------------------------------------------

        if (user.SchoolId.HasValue)
        {
            return (
                true,
                user.SchoolId.Value,
                null
            );
        }

        // ------------------------------------------------------------
        // TEACHER
        // ------------------------------------------------------------

        var teacher = await _context.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.UserId == userId);

        if (teacher != null)
        {
            return (
                true,
                teacher.SchoolId,
                null
            );
        }

        // ------------------------------------------------------------
        // STUDENT
        // ------------------------------------------------------------

        var student = await _context.StudentProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.UserId == userId);

        if (student != null)
        {
            return (
                true,
                student.SchoolId,
                null
            );
        }

        // ------------------------------------------------------------
        // PARENT
        // ------------------------------------------------------------

        // If Parent has a profile/table, resolve the school here.
        // Otherwise, if your ParentId relationship is on StudentProfile,
        // school will be resolved when getting children.

        return (
            false,
            null,
            "User is not associated with a school."
        );
    }

    // ========================================================================
    // CURRENT ACADEMIC PERIOD
    // ========================================================================

    private async Task<(bool Success, string? Session, string? Term, string? Error)>
        GetCurrentAcademicPeriodAsync(Guid schoolId)
    {
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
                null,
                "There is no active academic session."
            );
        }

        return (
            true,
            period.Session,
            period.Term,
            null
        );
    }

    // ========================================================================
    // AUTHORIZE RESULT MANAGEMENT
    //
    // SuperAdmin:
    //      Can manage results in their school.
    //
    // Admin:
    //      Can manage results in their school.
    //
    // Teacher:
    //      Can manage results only where they teach the class AND subject.
    //
    // Student:
    //      Cannot manage.
    //
    // Parent:
    //      Cannot manage.
    // ========================================================================

    private async Task<(bool Allowed, string? Error)>
        CanManageResultAsync(
            string userId,
            Guid schoolId,
            Guid classId,
            Guid subjectId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return (
                false,
                "User not found."
            );
        }

        var roles = await _userManager.GetRolesAsync(user);

        var isSuperAdmin =
            roles.Contains("SuperAdmin");

        var isAdmin =
            roles.Contains("Admin");

        var isTeacher =
            roles.Contains("Teacher");

        // ====================================================================
        // SUPER ADMIN / ADMIN
        // ====================================================================

        if (isSuperAdmin || isAdmin)
        {
            var validClass = await _context.Classes
                .AsNoTracking()
                .AnyAsync(x =>
                    x.Id == classId &&
                    x.SchoolId == schoolId);

            if (!validClass)
            {
                return (
                    false,
                    "Class not found in this school."
                );
            }

            var validSubject = await _context.Subjects
                .AsNoTracking()
                .AnyAsync(x =>
                    x.Id == subjectId &&
                    x.SchoolId == schoolId);

            if (!validSubject)
            {
                return (
                    false,
                    "Subject not found in this school."
                );
            }

            return (
                true,
                null
            );
        }

        // ====================================================================
        // TEACHER
        // ====================================================================

        if (isTeacher)
        {
            var teacher = await _context.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.SchoolId == schoolId);

            if (teacher == null)
            {
                return (
                    false,
                    "Teacher profile not found."
                );
            }

            var teachesClass = await _context.TeacherClasses
                .AsNoTracking()
                .AnyAsync(x =>
                    x.TeacherId == teacher.Id &&
                    x.ClassId == classId &&
                    x.Class.SchoolId == schoolId);

            if (!teachesClass)
            {
                return (
                    false,
                    "You are not assigned to this class."
                );
            }

            var teachesSubject = await _context.TeacherSubjects
                .AsNoTracking()
                .AnyAsync(x =>
                    x.TeacherId == teacher.Id &&
                    x.SubjectId == subjectId &&
                    x.Subject.SchoolId == schoolId);

            if (!teachesSubject)
            {
                return (
                    false,
                    "You are not assigned to this subject."
                );
            }

            return (
                true,
                null
            );
        }

        // ====================================================================
        // STUDENT / PARENT
        // ====================================================================

        return (
            false,
            "You are not authorized to modify results."
        );
    }

    // ========================================================================
    // GET SUBJECT
    // ========================================================================

    private async Task<object?> GetSubjectAsync(
        Guid schoolId,
        Guid subjectId)
    {
        return await _context.Subjects
            .AsNoTracking()
            .Where(x =>
                x.Id == subjectId &&
                x.SchoolId == schoolId)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Code
            })
            .FirstOrDefaultAsync();
    }

    // ========================================================================
    // GET STUDENTS
    // ========================================================================

    private async Task<List<StudentResultStudentDto>>
        GetStudentsAsync(
            Guid schoolId,
            Guid classId,
            List<Guid> studentIds)
    {
        return await _context.StudentProfiles
            .AsNoTracking()
            .Where(x =>
                studentIds.Contains(x.Id) &&
                x.SchoolId == schoolId &&
                x.ClassId == classId)
            .Select(x => new StudentResultStudentDto
            {
                Id = x.Id,
                ClassId = x.ClassId,

                StudentName = x.User.FullName,
                StudentNumber = x.StudentNumber
            })
            .ToListAsync();
    }

    // ========================================================================
    // BULK RESULT VALIDATION
    // ========================================================================

    private async Task<(bool Success, string? Error)>
        ValidateBulkStudentsAsync(
            Guid schoolId,
            Guid classId,
            List<Guid> studentIds)
    {
        var foundStudentIds = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x =>
                studentIds.Contains(x.Id) &&
                x.SchoolId == schoolId &&
                x.ClassId == classId)
            .Select(x => x.Id)
            .ToListAsync();

        var found = foundStudentIds.ToHashSet();

        var invalidStudents = studentIds
            .Where(x => !found.Contains(x))
            .ToList();

        if (invalidStudents.Count > 0)
        {
            return (
                false,
                "One or more students were not found in the selected class."
            );
        }

        return (
            true,
            null
        );
    }

    // ========================================================================
    // BUILD RESULT RESPONSE
    // ========================================================================

    private async Task<object?> BuildResultResponseAsync(
        StudentResult result)
    {
        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x =>
                x.Id == result.StudentId &&
                x.SchoolId == result.SchoolId)
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
                x.SchoolId == result.SchoolId)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Code
            })
            .FirstOrDefaultAsync();

        return new
        {
            resultId = result.Id,

            schoolId = result.SchoolId,

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

            createdAt = result.CreatedAt
        };
    }

    // ========================================================================
    // BULK EXAM RESULT
    // TEST = 40
    // EXAM = 60
    // ========================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        BulkExamResultAsync(
            string userId,
            BulkExamResultRequest request)
    {
        // ====================================================================
        // GET USER
        // ====================================================================

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return (
                false,
                null,
                "User not found."
            );
        }

        var roles = await _userManager.GetRolesAsync(user);

        var isSuperAdmin = roles.Contains("SuperAdmin");
        var isAdmin = roles.Contains("Admin");
        var isTeacher = roles.Contains("Teacher");

        if (!isSuperAdmin &&
            !isAdmin &&
            !isTeacher)
        {
            return (
                false,
                null,
                "You are not authorized to upload results."
            );
        }

        // ====================================================================
        // SCHOOL
        // ====================================================================

        var schoolResult =
            await GetUserSchoolAsync(userId);

        if (!schoolResult.Success ||
            !schoolResult.SchoolId.HasValue)
        {
            return (
                false,
                null,
                schoolResult.Error
            );
        }

        var schoolId =
            schoolResult.SchoolId.Value;

        // ====================================================================
        // CURRENT PERIOD
        // ====================================================================

        var period =
            await GetCurrentAcademicPeriodAsync(schoolId);

        if (!period.Success)
        {
            return (
                false,
                null,
                period.Error
            );
        }

        var currentSession = period.Session!;
        var currentTerm = period.Term!;

        // ====================================================================
        // AUTHORIZE
        // ====================================================================

        var authorization =
            await CanManageResultAsync(
                userId,
                schoolId,
                request.ClassId,
                request.SubjectId);

        if (!authorization.Allowed)
        {
            return (
                false,
                null,
                authorization.Error
            );
        }

        // ====================================================================
        // VALIDATE REQUEST
        // ====================================================================

        if (request.Results == null ||
            request.Results.Count == 0)
        {
            return (
                false,
                null,
                "At least one student result is required."
            );
        }

        // ====================================================================
        // DUPLICATES
        // ====================================================================

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

        // ====================================================================
        // VALIDATE EXAM
        // ====================================================================

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

        // ====================================================================
        // STUDENTS
        // ====================================================================

        var studentIds = request.Results
            .Select(x => x.StudentId)
            .ToList();

        var studentValidation =
            await ValidateBulkStudentsAsync(
                schoolId,
                request.ClassId,
                studentIds);

        if (!studentValidation.Success)
        {
            return (
                false,
                null,
                studentValidation.Error
            );
        }

        var students =
            await GetStudentsAsync(
                schoolId,
                request.ClassId,
                studentIds);

        var studentDictionary =
            students.ToDictionary(x => x.Id);

        // ====================================================================
        // SUBJECT
        // ====================================================================

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

        // ====================================================================
        // EXISTING RESULTS
        // ====================================================================

        var existingResults = await _context.StudentResults
            .Where(x =>
                x.SchoolId == schoolId &&
                x.ClassId == request.ClassId &&
                x.SubjectId == request.SubjectId &&
                x.Session == currentSession &&
                x.Term == currentTerm &&
                studentIds.Contains(x.StudentId))
            .ToListAsync();

        var existingByStudent =
            existingResults.ToDictionary(
                x => x.StudentId);

        var uploadedResults =
            new List<object>();

        // ====================================================================
        // PROCESS
        // ====================================================================

        foreach (var item in request.Results)
        {
            if (existingByStudent.TryGetValue(
                    item.StudentId,
                    out var result))
            {
                var testScore =
                    result.TestScore ?? 0m;

                result.ExamScore =
                    item.ExamScore;

                result.Score =
                    testScore +
                    item.ExamScore;

                result.Grade =
                    CalculateGrade(result.Score);

                result.Remark =
                    item.Remark;

                var student =
                    studentDictionary[item.StudentId];

                uploadedResults.Add(new
                {
                    resultId = result.Id,

                    studentId = result.StudentId,
                    studentName = student.StudentName,
                    studentNumber = student.StudentNumber,

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
                var totalScore =
                    item.ExamScore;

                var newResult =
                    new Models.Results.StudentResult
                    {
                        Id = Guid.NewGuid(),

                        StudentId = item.StudentId,
                        SchoolId = schoolId,

                        ClassId = request.ClassId,
                        SubjectId = request.SubjectId,

                        Session = currentSession,
                        Term = currentTerm,

                        TestScore = 0m,
                        ExamScore = item.ExamScore,

                        Score = totalScore,

                        Grade =
                            CalculateGrade(totalScore),

                        Remark = item.Remark,

                        CreatedAt =
                            DateTime.UtcNow
                    };

                _context.StudentResults.Add(
                    newResult);

                var student =
                    studentDictionary[item.StudentId];

                uploadedResults.Add(new
                {
                    resultId = newResult.Id,

                    studentId = newResult.StudentId,
                    studentName = student.StudentName,
                    studentNumber = student.StudentNumber,

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

        await _context.SaveChangesAsync();

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

                uploadedCount =
                    uploadedResults.Count,

                results =
                    uploadedResults
            },
            null
        );
    }

    // ========================================================================
    // BULK TEST RESULT
    // ========================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        BulkTestResultAsync(
            string userId,
            BulkTestResultRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return (
                false,
                null,
                "User not found."
            );
        }

        var roles = await _userManager.GetRolesAsync(user);

        var isSuperAdmin = roles.Contains("SuperAdmin");
        var isAdmin = roles.Contains("Admin");
        var isTeacher = roles.Contains("Teacher");

        if (!isSuperAdmin &&
            !isAdmin &&
            !isTeacher)
        {
            return (
                false,
                null,
                "You are not authorized to upload results."
            );
        }

        var schoolResult =
            await GetUserSchoolAsync(userId);

        if (!schoolResult.Success ||
            !schoolResult.SchoolId.HasValue)
        {
            return (
                false,
                null,
                schoolResult.Error
            );
        }

        var schoolId =
            schoolResult.SchoolId.Value;

        var period =
            await GetCurrentAcademicPeriodAsync(schoolId);

        if (!period.Success)
        {
            return (
                false,
                null,
                period.Error
            );
        }

        var currentSession = period.Session!;
        var currentTerm = period.Term!;

        var authorization =
            await CanManageResultAsync(
                userId,
                schoolId,
                request.ClassId,
                request.SubjectId);

        if (!authorization.Allowed)
        {
            return (
                false,
                null,
                authorization.Error
            );
        }

        if (request.Results == null ||
            request.Results.Count == 0)
        {
            return (
                false,
                null,
                "At least one student result is required."
            );
        }

        var duplicateStudentIds =
            request.Results
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

        var invalidScore =
            request.Results
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

        var studentIds =
            request.Results
                .Select(x => x.StudentId)
                .ToList();

        var studentValidation =
            await ValidateBulkStudentsAsync(
                schoolId,
                request.ClassId,
                studentIds);

        if (!studentValidation.Success)
        {
            return (
                false,
                null,
                studentValidation.Error
            );
        }

        var students =
            await GetStudentsAsync(
                schoolId,
                request.ClassId,
                studentIds);

        var studentDictionary =
            students.ToDictionary(x => x.Id);

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

        var existingResults =
            await _context.StudentResults
                .Where(x =>
                    x.SchoolId == schoolId &&
                    x.ClassId == request.ClassId &&
                    x.SubjectId == request.SubjectId &&
                    x.Session == currentSession &&
                    x.Term == currentTerm &&
                    studentIds.Contains(x.StudentId))
                .ToListAsync();

        var existingByStudent =
            existingResults.ToDictionary(
                x => x.StudentId);

        var uploadedResults =
            new List<object>();

        foreach (var item in request.Results)
        {
            if (existingByStudent.TryGetValue(
                    item.StudentId,
                    out var result))
            {
                var examScore =
                    result.ExamScore ?? 0m;

                result.TestScore =
                    item.TestScore;

                result.Score =
                    item.TestScore +
                    examScore;

                result.Grade =
                    CalculateGrade(result.Score);

                result.Remark =
                    item.Remark;

                var student =
                    studentDictionary[item.StudentId];

                uploadedResults.Add(new
                {
                    resultId = result.Id,

                    studentId = result.StudentId,
                    studentName = student.StudentName,
                    studentNumber = student.StudentNumber,

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
                var totalScore =
                    item.TestScore;

                var newResult =
                    new Models.Results.StudentResult
                    {
                        Id = Guid.NewGuid(),

                        StudentId = item.StudentId,
                        SchoolId = schoolId,

                        ClassId = request.ClassId,
                        SubjectId = request.SubjectId,

                        Session = currentSession,
                        Term = currentTerm,

                        TestScore =
                            item.TestScore,

                        ExamScore = 0m,

                        Score = totalScore,

                        Grade =
                            CalculateGrade(totalScore),

                        Remark =
                            item.Remark,

                        CreatedAt =
                            DateTime.UtcNow
                    };

                _context.StudentResults.Add(
                    newResult);

                var student =
                    studentDictionary[item.StudentId];

                uploadedResults.Add(new
                {
                    resultId = newResult.Id,

                    studentId = newResult.StudentId,
                    studentName = student.StudentName,
                    studentNumber = student.StudentNumber,

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

        await _context.SaveChangesAsync();

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

                uploadedCount =
                    uploadedResults.Count,

                results =
                    uploadedResults
            },
            null
        );
    }

    // ========================================================================
    // BULK TEST + EXAM RESULT
    // ========================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        BulkResultAsync(
            string userId,
            BulkResultRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return (
                false,
                null,
                "User not found."
            );
        }

        var roles = await _userManager.GetRolesAsync(user);

        var isSuperAdmin = roles.Contains("SuperAdmin");
        var isAdmin = roles.Contains("Admin");
        var isTeacher = roles.Contains("Teacher");

        if (!isSuperAdmin &&
            !isAdmin &&
            !isTeacher)
        {
            return (
                false,
                null,
                "You are not authorized to upload results."
            );
        }

        var schoolResult =
            await GetUserSchoolAsync(userId);

        if (!schoolResult.Success ||
            !schoolResult.SchoolId.HasValue)
        {
            return (
                false,
                null,
                schoolResult.Error
            );
        }

        var schoolId =
            schoolResult.SchoolId.Value;

        var period =
            await GetCurrentAcademicPeriodAsync(schoolId);

        if (!period.Success)
        {
            return (
                false,
                null,
                period.Error
            );
        }

        var currentSession = period.Session!;
        var currentTerm = period.Term!;

        var authorization =
            await CanManageResultAsync(
                userId,
                schoolId,
                request.ClassId,
                request.SubjectId);

        if (!authorization.Allowed)
        {
            return (
                false,
                null,
                authorization.Error
            );
        }

        if (request.Results == null ||
            request.Results.Count == 0)
        {
            return (
                false,
                null,
                "At least one student result is required."
            );
        }

        var duplicateStudentIds =
            request.Results
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

        var invalidTestScore =
            request.Results
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

        var invalidExamScore =
            request.Results
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

        var studentIds =
            request.Results
                .Select(x => x.StudentId)
                .ToList();

        var studentValidation =
            await ValidateBulkStudentsAsync(
                schoolId,
                request.ClassId,
                studentIds);

        if (!studentValidation.Success)
        {
            return (
                false,
                null,
                studentValidation.Error
            );
        }

        var students =
            await GetStudentsAsync(
                schoolId,
                request.ClassId,
                studentIds);

        var studentDictionary =
            students.ToDictionary(x => x.Id);

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

        var existingResults =
            await _context.StudentResults
                .Where(x =>
                    x.SchoolId == schoolId &&
                    x.ClassId == request.ClassId &&
                    x.SubjectId == request.SubjectId &&
                    x.Session == currentSession &&
                    x.Term == currentTerm &&
                    studentIds.Contains(x.StudentId))
                .ToListAsync();

        var existingByStudent =
            existingResults.ToDictionary(
                x => x.StudentId);

        var uploadedResults =
            new List<object>();

        foreach (var item in request.Results)
        {
            var totalScore =
                item.TestScore +
                item.ExamScore;

            var grade =
                CalculateGrade(totalScore);

            if (existingByStudent.TryGetValue(
                    item.StudentId,
                    out var result))
            {
                result.TestScore =
                    item.TestScore;

                result.ExamScore =
                    item.ExamScore;

                result.Score =
                    totalScore;

                result.Grade =
                    grade;

                result.Remark =
                    item.Remark;

                var student =
                    studentDictionary[item.StudentId];

                uploadedResults.Add(new
                {
                    resultId = result.Id,

                    studentId = result.StudentId,
                    studentName = student.StudentName,
                    studentNumber = student.StudentNumber,

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
                var newResult =
                    new Models.Results.StudentResult
                    {
                        Id = Guid.NewGuid(),

                        StudentId = item.StudentId,
                        SchoolId = schoolId,

                        ClassId = request.ClassId,
                        SubjectId = request.SubjectId,

                        Session = currentSession,
                        Term = currentTerm,

                        TestScore =
                            item.TestScore,

                        ExamScore =
                            item.ExamScore,

                        Score =
                            totalScore,

                        Grade =
                            grade,

                        Remark =
                            item.Remark,

                        CreatedAt =
                            DateTime.UtcNow
                    };

                _context.StudentResults.Add(
                    newResult);

                var student =
                    studentDictionary[item.StudentId];

                uploadedResults.Add(new
                {
                    resultId = newResult.Id,

                    studentId = newResult.StudentId,
                    studentName = student.StudentName,
                    studentNumber = student.StudentNumber,

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

        await _context.SaveChangesAsync();

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

                uploadedCount =
                    uploadedResults.Count,

                results =
                    uploadedResults
            },
            null
        );
    }

    // ========================================================================
    // UPLOAD TEST RESULT
    // ========================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        UploadTestResultAsync(
            string userId,
            UploadTestResultRequest request)
    {
        var authorization =
            await AuthorizeUploadRequestAsync(
                userId,
                request.ClassId,
                request.SubjectId);

        if (!authorization.Success)
        {
            return (
                false,
                null,
                authorization.Error
            );
        }

        var schoolId =
            authorization.SchoolId!.Value;

        var currentSession =
            authorization.Session!;

        var currentTerm =
            authorization.Term!;

        if (request.TestScore < 0 ||
            request.TestScore > 40)
        {
            return (
                false,
                null,
                "Test score must be between 0 and 40."
            );
        }

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

        if (student.ClassId != request.ClassId)
        {
            return (
                false,
                null,
                "The selected class does not belong to this student."
            );
        }

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

        var result =
            await _context.StudentResults
                .FirstOrDefaultAsync(x =>
                    x.StudentId == request.StudentId &&
                    x.SchoolId == schoolId &&
                    x.ClassId == request.ClassId &&
                    x.SubjectId == request.SubjectId &&
                    x.Session == currentSession &&
                    x.Term == currentTerm);

        if (result != null)
        {
            var examScore =
                result.ExamScore ?? 0m;

            result.TestScore =
                request.TestScore;

            result.Score =
                request.TestScore +
                examScore;

            result.Grade =
                CalculateGrade(result.Score);

            result.Remark =
                request.Remark;

            await _context.SaveChangesAsync();

            return (
                true,
                await BuildResultResponseAsync(result),
                null
            );
        }

        var newResult =
            new Models.Results.StudentResult
            {
                Id = Guid.NewGuid(),

                StudentId = request.StudentId,
                SchoolId = schoolId,

                ClassId = request.ClassId,
                SubjectId = request.SubjectId,

                Session = currentSession,
                Term = currentTerm,

                TestScore =
                    request.TestScore,

                ExamScore = 0m,

                Score =
                    request.TestScore,

                Grade =
                    CalculateGrade(request.TestScore),

                Remark =
                    request.Remark,

                CreatedAt =
                    DateTime.UtcNow
            };

        _context.StudentResults.Add(newResult);

        await _context.SaveChangesAsync();

        return (
            true,
            await BuildResultResponseAsync(newResult),
            null
        );
    }

    // ========================================================================
    // UPLOAD EXAM RESULT
    // ========================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        UploadExamResultAsync(
            string userId,
            UploadExamResultRequest request)
    {
        var authorization =
            await AuthorizeUploadRequestAsync(
                userId,
                request.ClassId,
                request.SubjectId);

        if (!authorization.Success)
        {
            return (
                false,
                null,
                authorization.Error
            );
        }

        var schoolId =
            authorization.SchoolId!.Value;

        var currentSession =
            authorization.Session!;

        var currentTerm =
            authorization.Term!;

        if (request.ExamScore < 0 ||
            request.ExamScore > 60)
        {
            return (
                false,
                null,
                "Exam score must be between 0 and 60."
            );
        }

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

        if (student.ClassId != request.ClassId)
        {
            return (
                false,
                null,
                "The selected class does not belong to this student."
            );
        }

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

        var result =
            await _context.StudentResults
                .FirstOrDefaultAsync(x =>
                    x.StudentId == request.StudentId &&
                    x.SchoolId == schoolId &&
                    x.ClassId == request.ClassId &&
                    x.SubjectId == request.SubjectId &&
                    x.Session == currentSession &&
                    x.Term == currentTerm);

        if (result != null)
        {
            var testScore =
                result.TestScore ?? 0m;

            result.ExamScore =
                request.ExamScore;

            result.Score =
                testScore +
                request.ExamScore;

            result.Grade =
                CalculateGrade(result.Score);

            result.Remark =
                request.Remark;

            await _context.SaveChangesAsync();

            return (
                true,
                await BuildResultResponseAsync(result),
                null
            );
        }

        var newResult =
            new Models.Results.StudentResult
            {
                Id = Guid.NewGuid(),

                StudentId = request.StudentId,
                SchoolId = schoolId,

                ClassId = request.ClassId,
                SubjectId = request.SubjectId,

                Session = currentSession,
                Term = currentTerm,

                TestScore = 0m,

                ExamScore =
                    request.ExamScore,

                Score =
                    request.ExamScore,

                Grade =
                    CalculateGrade(
                        request.ExamScore),

                Remark =
                    request.Remark,

                CreatedAt =
                    DateTime.UtcNow
            };

        _context.StudentResults.Add(newResult);

        await _context.SaveChangesAsync();

        return (
            true,
            await BuildResultResponseAsync(newResult),
            null
        );
    }

    // ========================================================================
    // UPLOAD COMPLETE RESULT
    // TEST = 40
    // EXAM = 60
    // TOTAL = 100
    // ========================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        UploadResultAsync(
            string userId,
            UploadResultRequest request)
    {
        var authorization =
            await AuthorizeUploadRequestAsync(
                userId,
                request.ClassId,
                request.SubjectId);

        if (!authorization.Success)
        {
            return (
                false,
                null,
                authorization.Error
            );
        }

        var schoolId =
            authorization.SchoolId!.Value;

        var currentSession =
            authorization.Session!;

        var currentTerm =
            authorization.Term!;

        if (request.TestScore < 0 ||
            request.TestScore > 40)
        {
            return (
                false,
                null,
                "Test score must be between 0 and 40."
            );
        }

        if (request.ExamScore < 0 ||
            request.ExamScore > 60)
        {
            return (
                false,
                null,
                "Exam score must be between 0 and 60."
            );
        }

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

        if (student.ClassId != request.ClassId)
        {
            return (
                false,
                null,
                "The selected class does not belong to this student."
            );
        }

        var result =
            await _context.StudentResults
                .FirstOrDefaultAsync(x =>
                    x.StudentId == request.StudentId &&
                    x.SchoolId == schoolId &&
                    x.ClassId == request.ClassId &&
                    x.SubjectId == request.SubjectId &&
                    x.Session == currentSession &&
                    x.Term == currentTerm);

        var totalScore =
            request.TestScore +
            request.ExamScore;

        var grade =
            CalculateGrade(totalScore);

        if (result != null)
        {
            result.TestScore =
                request.TestScore;

            result.ExamScore =
                request.ExamScore;

            result.Score =
                totalScore;

            result.Grade =
                grade;

            result.Remark =
                request.Remark;

            await _context.SaveChangesAsync();

            return (
                true,
                await BuildResultResponseAsync(result),
                null
            );
        }

        var newResult =
            new Models.Results.StudentResult
            {
                Id = Guid.NewGuid(),

                StudentId =
                    request.StudentId,

                SchoolId =
                    schoolId,

                ClassId =
                    request.ClassId,

                SubjectId =
                    request.SubjectId,

                Session =
                    currentSession,

                Term =
                    currentTerm,

                TestScore =
                    request.TestScore,

                ExamScore =
                    request.ExamScore,

                Score =
                    totalScore,

                Grade =
                    grade,

                Remark =
                    request.Remark,

                CreatedAt =
                    DateTime.UtcNow
            };

        _context.StudentResults.Add(
            newResult);

        await _context.SaveChangesAsync();

        return (
            true,
            await BuildResultResponseAsync(newResult),
            null
        );
    }

    // ========================================================================
    // AUTHORIZE UPLOAD
    // ========================================================================

    private async Task<(bool Success, Guid? SchoolId, string? Session, string? Term, string? Error)>
        AuthorizeUploadRequestAsync(
            string userId,
            Guid classId,
            Guid subjectId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return (
                false,
                null,
                null,
                null,
                "User not found."
            );
        }

        var roles =
            await _userManager.GetRolesAsync(user);

        var isSuperAdmin =
            roles.Contains("SuperAdmin");

        var isAdmin =
            roles.Contains("Admin");

        var isTeacher =
            roles.Contains("Teacher");

        if (!isSuperAdmin &&
            !isAdmin &&
            !isTeacher)
        {
            return (
                false,
                null,
                null,
                null,
                "You are not authorized to modify results."
            );
        }

        var school =
            await GetUserSchoolAsync(userId);

        if (!school.Success ||
            !school.SchoolId.HasValue)
        {
            return (
                false,
                null,
                null,
                null,
                school.Error
            );
        }

        var schoolId =
            school.SchoolId.Value;

        var period =
            await GetCurrentAcademicPeriodAsync(
                schoolId);

        if (!period.Success)
        {
            return (
                false,
                null,
                null,
                null,
                period.Error
            );
        }

        var authorization =
            await CanManageResultAsync(
                userId,
                schoolId,
                classId,
                subjectId);

        if (!authorization.Allowed)
        {
            return (
                false,
                null,
                null,
                null,
                authorization.Error
            );
        }

        return (
            true,
            schoolId,
            period.Session,
            period.Term,
            null
        );
    }

    // ========================================================================
    // UPDATE TEST RESULT
    // ========================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        UpdateTestResultAsync(
            string userId,
            Guid resultId,
            UpdateTestResultRequest request)
    {
        var authorization =
            await AuthorizeExistingResultAsync(
                userId,
                resultId);

        if (!authorization.Success)
        {
            return (
                false,
                null,
                authorization.Error
            );
        }

        var result =
            authorization.Result!;

        if (request.TestScore < 0 ||
            request.TestScore > 40)
        {
            return (
                false,
                null,
                "Test score must be between 0 and 40."
            );
        }

        var examScore =
            result.ExamScore ?? 0m;

        result.TestScore =
            request.TestScore;

        result.Score =
            request.TestScore +
            examScore;

        result.Grade =
            CalculateGrade(result.Score);

        result.Remark =
            request.Remark;

        await _context.SaveChangesAsync();

        return (
            true,
            await BuildResultResponseAsync(result),
            null
        );
    }

    // ========================================================================
    // UPDATE EXAM RESULT
    // ========================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        UpdateExamResultAsync(
            string userId,
            Guid resultId,
            UpdateExamResultRequest request)
    {
        var authorization =
            await AuthorizeExistingResultAsync(
                userId,
                resultId);

        if (!authorization.Success)
        {
            return (
                false,
                null,
                authorization.Error
            );
        }

        var result =
            authorization.Result!;

        if (request.ExamScore < 0 ||
            request.ExamScore > 60)
        {
            return (
                false,
                null,
                "Exam score must be between 0 and 60."
            );
        }

        var testScore =
            result.TestScore ?? 0m;

        result.ExamScore =
            request.ExamScore;

        result.Score =
            testScore +
            request.ExamScore;

        result.Grade =
            CalculateGrade(result.Score);

        result.Remark =
            request.Remark;

        await _context.SaveChangesAsync();

        return (
            true,
            await BuildResultResponseAsync(result),
            null
        );
    }

    // ========================================================================
    // UPDATE COMPLETE RESULT
    // ========================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        UpdateResultAsync(
            string userId,
            Guid resultId,
            UpdateResultRequest request)
    {
        var authorization =
            await AuthorizeExistingResultAsync(
                userId,
                resultId);

        if (!authorization.Success)
        {
            return (
                false,
                null,
                authorization.Error
            );
        }

        var result =
            authorization.Result!;

        if (request.TestScore < 0 ||
            request.TestScore > 40)
        {
            return (
                false,
                null,
                "Test score must be between 0 and 40."
            );
        }

        if (request.ExamScore < 0 ||
            request.ExamScore > 60)
        {
            return (
                false,
                null,
                "Exam score must be between 0 and 60."
            );
        }

        result.TestScore =
            request.TestScore;

        result.ExamScore =
            request.ExamScore;

        result.Score =
            request.TestScore +
            request.ExamScore;

        result.Grade =
            CalculateGrade(result.Score);

        result.Remark =
            request.Remark;

        await _context.SaveChangesAsync();

        return (
            true,
            await BuildResultResponseAsync(result),
            null
        );
    }

    // ========================================================================
    // AUTHORIZE EXISTING RESULT
    // ========================================================================

    private async Task<(bool Success, StudentResult? Result, string? Error)>
        AuthorizeExistingResultAsync(
            string userId,
            Guid resultId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return (
                false,
                null,
                "User not found."
            );
        }

        var roles =
            await _userManager.GetRolesAsync(user);

        var isSuperAdmin =
            roles.Contains("SuperAdmin");

        var isAdmin =
            roles.Contains("Admin");

        var isTeacher =
            roles.Contains("Teacher");

        if (!isSuperAdmin &&
            !isAdmin &&
            !isTeacher)
        {
            return (
                false,
                null,
                "You are not authorized to edit results."
            );
        }

        var schoolResult =
            await GetUserSchoolAsync(userId);

        if (!schoolResult.Success ||
            !schoolResult.SchoolId.HasValue)
        {
            return (
                false,
                null,
                schoolResult.Error
            );
        }

        var schoolId =
            schoolResult.SchoolId.Value;

        var period =
            await GetCurrentAcademicPeriodAsync(
                schoolId);

        if (!period.Success)
        {
            return (
                false,
                null,
                period.Error
            );
        }

        var result =
            await _context.StudentResults
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

        // ====================================================================
        // CURRENT PERIOD ONLY
        // ====================================================================

        if (result.Session != period.Session ||
            result.Term != period.Term)
        {
            return (
                false,
                null,
                "Only results for the current academic period can be edited."
            );
        }

        // ====================================================================
        // CLASS + SUBJECT AUTHORIZATION
        // ====================================================================

        var authorization =
            await CanManageResultAsync(
                userId,
                schoolId,
                result.ClassId,
                result.SubjectId);

        if (!authorization.Allowed)
        {
            return (
                false,
                null,
                authorization.Error
            );
        }

        return (
            true,
            result,
            null
        );
    }

    // ========================================================================
    // GET TEACHER / ADMIN / STUDENT / PARENT RESULTS
    // ========================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetResultsAsync(
            string userId,
            GetTeacherResultsRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return (
                false,
                null,
                "User not found."
            );
        }

        var roles =
            await _userManager.GetRolesAsync(user);

        var isSuperAdmin =
            roles.Contains("SuperAdmin");

        var isAdmin =
            roles.Contains("Admin");

        var isTeacher =
            roles.Contains("Teacher");

        var isStudent =
            roles.Contains("Student");

        var isParent =
            roles.Contains("Parent");

        if (!isSuperAdmin &&
            !isAdmin &&
            !isTeacher &&
            !isStudent &&
            !isParent)
        {
            return (
                false,
                null,
                "You are not authorized to view results."
            );
        }

        // ====================================================================
        // SCHOOL
        // ====================================================================

        var schoolResult =
            await GetUserSchoolAsync(userId);

        Guid schoolId;

        if (schoolResult.Success &&
            schoolResult.SchoolId.HasValue)
        {
            schoolId =
                schoolResult.SchoolId.Value;
        }
        else if (isParent)
        {
            // Parent may not have SchoolId directly.
            // Resolve school from their children.

            var childSchoolId =
        await _context.ParentStudents
            .AsNoTracking()
            .Where(x => x.Parent.UserId == userId)
            .Select(x => (Guid?)x.Student.SchoolId)
            .FirstOrDefaultAsync();

            if (!childSchoolId.HasValue)
            {
                return (
                    false,
                    null,
                    "No students are associated with this parent."
                );
            }

            schoolId =
                childSchoolId.Value;
        }
        else
        {
            return (
                false,
                null,
                schoolResult.Error
            );
        }

        // ====================================================================
        // BASE QUERY
        // ====================================================================

        var query =
            _context.StudentResults
                .AsNoTracking()
                .Where(x =>
                    x.SchoolId == schoolId);

        // ====================================================================
        // TEACHER
        // ====================================================================

        if (isTeacher &&
            !isAdmin &&
            !isSuperAdmin)
        {
            var teacher =
                await _context.Teachers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.UserId == userId &&
                        x.SchoolId == schoolId);

            if (teacher == null)
            {
                return (
                    false,
                    null,
                    "Teacher profile not found."
                );
            }

            query =
                query.Where(x =>
                    x.Class.TeacherClasses.Any(tc =>
                        tc.TeacherId == teacher.Id) &&

                    x.Subject.TeacherSubjects.Any(ts =>
                        ts.TeacherId == teacher.Id));
        }

        // ====================================================================
        // STUDENT
        // ====================================================================

        if (isStudent)
        {
            var student =
                await _context.StudentProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.UserId == userId &&
                        x.SchoolId == schoolId);

            if (student == null)
            {
                return (
                    false,
                    null,
                    "Student profile not found."
                );
            }

            // Student can ONLY see own results.

            query =
                query.Where(x =>
                    x.StudentId == student.Id);
        }

        // ====================================================================
        // PARENT
        // ====================================================================

        if (isParent)
        {
            // ------------------------------------------------------------
            // IMPORTANT:
            //
            // Change ParentId if your parent/student relationship uses
            // another property or join table.
            // ------------------------------------------------------------

            var childIds =
             await _context.ParentStudents
                 .AsNoTracking()
                 .Where(x =>
                     x.Parent.UserId == userId &&
                     x.Student.SchoolId == schoolId)
                 .Select(x => x.StudentId)
                 .ToListAsync();





            if (childIds.Count == 0)
            {
                return (
                    true,
                    new
                    {
                        schoolId,
                        count = 0,
                        results = Array.Empty<object>()
                    },
                    null
                );
            }

            // Parent can ONLY see children's results.

            query =
                query.Where(x =>
                    childIds.Contains(
                        x.StudentId));
        }

        // ====================================================================
        // OPTIONAL STUDENT FILTER
        // ====================================================================

        if (request.StudentId.HasValue)
        {
            query =
                query.Where(x =>
                    x.StudentId ==
                    request.StudentId.Value);
        }

        // ====================================================================
        // OPTIONAL CLASS FILTER
        // ====================================================================

        if (request.ClassId.HasValue)
        {
            // Teachers can only query classes they teach.
            if (isTeacher &&
                !isAdmin &&
                !isSuperAdmin)
            {
                var teacher =
                    await _context.Teachers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.UserId == userId &&
                            x.SchoolId == schoolId);

                if (teacher == null)
                {
                    return (
                        false,
                        null,
                        "Teacher profile not found."
                    );
                }

                var teachesClass =
                    await _context.TeacherClasses
                        .AsNoTracking()
                        .AnyAsync(x =>
                            x.TeacherId == teacher.Id &&
                            x.ClassId ==
                                request.ClassId.Value &&
                            x.Class.SchoolId ==
                                schoolId);

                if (!teachesClass)
                {
                    return (
                        false,
                        null,
                        "You are not assigned to this class."
                    );
                }
            }

            query =
                query.Where(x =>
                    x.ClassId ==
                    request.ClassId.Value);
        }

        // ====================================================================
        // OPTIONAL SUBJECT FILTER
        // ====================================================================

        if (request.SubjectId.HasValue)
        {
            if (isTeacher &&
                !isAdmin &&
                !isSuperAdmin)
            {
                var teacher =
                    await _context.Teachers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.UserId == userId &&
                            x.SchoolId == schoolId);

                if (teacher == null)
                {
                    return (
                        false,
                        null,
                        "Teacher profile not found."
                    );
                }

                var teachesSubject =
                    await _context.TeacherSubjects
                        .AsNoTracking()
                        .AnyAsync(x =>
                            x.TeacherId == teacher.Id &&
                            x.SubjectId ==
                                request.SubjectId.Value &&
                            x.Subject.SchoolId ==
                                schoolId);

                if (!teachesSubject)
                {
                    return (
                        false,
                        null,
                        "You are not assigned to this subject."
                    );
                }
            }

            query =
                query.Where(x =>
                    x.SubjectId ==
                    request.SubjectId.Value);
        }

        // ====================================================================
        // SESSION
        // ====================================================================

        if (!string.IsNullOrWhiteSpace(
                request.Session))
        {
            query =
                query.Where(x =>
                    x.Session ==
                    request.Session);
        }

        // ====================================================================
        // TERM
        // ====================================================================

        if (!string.IsNullOrWhiteSpace(
                request.Term))
        {
            query =
                query.Where(x =>
                    x.Term ==
                    request.Term);
        }

        // ====================================================================
        // GET RESULTS
        // ====================================================================

        var results =
            await query
                .OrderBy(x =>
                    x.Student.User.FullName)
                .ThenBy(x =>
                    x.Subject.Name)
                .Select(x => new
                {
                    resultId = x.Id,

                    schoolId = x.SchoolId,

                    studentId = x.StudentId,
                    studentName =
                        x.Student.User.FullName,
                    studentNumber =
                        x.Student.StudentNumber,

                    classId = x.ClassId,
                    className =
                        x.Class.Name,

                    subjectId =
                        x.SubjectId,
                    subjectName =
                        x.Subject.Name,
                    subjectCode =
                        x.Subject.Code,

                    session =
                        x.Session,
                    term =
                        x.Term,

                    testScore =
                        x.TestScore,
                    examScore =
                        x.ExamScore,

                    score =
                        x.Score,
                    grade =
                        x.Grade,

                    remark =
                        x.Remark,

                    createdAt =
                        x.CreatedAt
                })
                .ToListAsync();

        // ====================================================================
        // CURRENT PERIOD
        // ====================================================================

        var currentPeriod =
            await GetCurrentAcademicPeriodAsync(
                schoolId);

        return (
            true,
            new
            {
                schoolId,

                currentSession =
                    currentPeriod.Session,

                currentTerm =
                    currentPeriod.Term,

                count =
                    results.Count,

                results
            },
            null
        );
    }

    // ========================================================================
    // DELETE RESULT
    // ========================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        DeleteResultAsync(
            string userId,
            Guid resultId)
    {
        var user =
            await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return (
                false,
                null,
                "User not found."
            );
        }

        var roles =
            await _userManager.GetRolesAsync(user);

        var isSuperAdmin =
            roles.Contains("SuperAdmin");

        var isAdmin =
            roles.Contains("Admin");

        var isTeacher =
            roles.Contains("Teacher");

        if (!isSuperAdmin &&
            !isAdmin &&
            !isTeacher)
        {
            return (
                false,
                null,
                "You are not authorized to delete results."
            );
        }

        var schoolResult =
            await GetUserSchoolAsync(userId);

        if (!schoolResult.Success ||
            !schoolResult.SchoolId.HasValue)
        {
            return (
                false,
                null,
                schoolResult.Error
            );
        }

        var schoolId =
            schoolResult.SchoolId.Value;

        var period =
            await GetCurrentAcademicPeriodAsync(
                schoolId);

        if (!period.Success)
        {
            return (
                false,
                null,
                period.Error
            );
        }

        var result =
            await _context.StudentResults
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

        // ====================================================================
        // CURRENT PERIOD ONLY
        // ====================================================================

        if (result.Session != period.Session ||
            result.Term != period.Term)
        {
            return (
                false,
                null,
                "Only results for the current academic period can be deleted."
            );
        }

        // ====================================================================
        // AUTHORIZATION
        // ====================================================================

        var authorization =
            await CanManageResultAsync(
                userId,
                schoolId,
                result.ClassId,
                result.SubjectId);

        if (!authorization.Allowed)
        {
            return (
                false,
                null,
                authorization.Error
            );
        }

        // ====================================================================
        // DELETE
        // ====================================================================

        _context.StudentResults.Remove(
            result);

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                resultId =
                    result.Id,

                studentId =
                    result.StudentId,

                classId =
                    result.ClassId,

                subjectId =
                    result.SubjectId,

                session =
                    result.Session,

                term =
                    result.Term
            },
            null
        );
    }


    // ========================================================================
    // DTO USED INTERNALLY
    // ========================================================================

    private class StudentResultStudentDto
    {
        public Guid Id { get; set; }

        public Guid ClassId { get; set; }

        public string? StudentName { get; set; }

        public string? StudentNumber { get; set; }
    }

    // ================================================================
    // PREVIEW EXCEL IMPORT
    // ================================================================

    public async Task<(
        bool Success,
        ResultImportPreviewResponse? Data,
        string? Error
    )> PreviewAsync(
        Guid schoolId,
        ImportResultsRequest request)
    {
        // ------------------------------------------------------------
        // VALIDATE SCHOOL
        // ------------------------------------------------------------

        var school = await _context.Schools
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == schoolId);

        if (school == null)
        {
            return (
                false,
                null,
                "School not found."
            );
        }

        // ------------------------------------------------------------
        // VALIDATE CLASS
        // ------------------------------------------------------------

        var classroom = await _context.Classes
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == request.ClassId &&
                x.SchoolId == schoolId);

        if (classroom == null)
        {
            return (
                false,
                null,
                "Class does not belong to this school."
            );
        }

        // ------------------------------------------------------------
        // VALIDATE SUBJECT
        // ------------------------------------------------------------

        var subject = await _context.Subjects
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == request.SubjectId &&
                x.SchoolId == schoolId);

        if (subject == null)
        {
            return (
                false,
                null,
                "Subject does not belong to this school."
            );
        }

        // ------------------------------------------------------------
        // VALIDATE FILE
        // ------------------------------------------------------------

        if (request.File == null ||
            request.File.Length == 0)
        {
            return (
                false,
                null,
                "Please upload a result file."
            );
        }

        var extension =
            Path.GetExtension(request.File.FileName)
                .ToLowerInvariant();

        if (extension != ".xlsx")
        {
            return (
                false,
                null,
                "Only .xlsx files are currently supported."
            );
        }

        // ------------------------------------------------------------
        // READ EXCEL
        // ------------------------------------------------------------

        using var stream =
            request.File.OpenReadStream();

        using var workbook =
            new XLWorkbook(stream);

        var worksheet =
            workbook.Worksheets.FirstOrDefault();

        if (worksheet == null)
        {
            return (
                false,
                null,
                "The Excel file contains no worksheet."
            );
        }

        var preview =
            new ResultImportPreviewResponse();

        var lastRow =
            worksheet.LastRowUsed()?.RowNumber() ?? 0;

        if (lastRow < 2)
        {
            return (
                false,
                null,
                "The Excel file contains no result rows."
            );
        }

        // ------------------------------------------------------------
        // PRELOAD STUDENTS
        // ------------------------------------------------------------

        var students = await _context.StudentProfiles
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x =>
                x.SchoolId == schoolId &&
                x.ClassId == request.ClassId)
            .ToDictionaryAsync(
                x => x.StudentNumber,
                x => x);

        // ------------------------------------------------------------
        // READ ROWS
        // ------------------------------------------------------------

        for (var rowNumber = 2;
             rowNumber <= lastRow;
             rowNumber++)
        {
            var row =
                worksheet.Row(rowNumber);

            var studentNumber =
                row.Cell(1)
                    .GetString()
                    .Trim();

            var testText =
                row.Cell(2)
                    .GetString()
                    .Trim();

            var examText =
                row.Cell(3)
                    .GetString()
                    .Trim();

            var resultRow =
                new ResultImportRow
                {
                    RowNumber = rowNumber,
                    StudentNumber = studentNumber
                };

            // --------------------------------------------------------
            // STUDENT
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(studentNumber))
            {
                resultRow.Errors.Add(
                    "Student number is required.");
            }
            else if (!students.TryGetValue(
                         studentNumber,
                         out var student))
            {
                resultRow.Errors.Add(
                    "Student was not found in the selected class.");
            }
            else
            {
                resultRow.StudentName =
                    student.User.FullName;
            }

            // --------------------------------------------------------
            // TEST SCORE
            // --------------------------------------------------------

            if (!decimal.TryParse(
                    testText,
                    out var testScore))
            {
                resultRow.Errors.Add(
                    "Test score must be a valid number.");
            }
            else if (testScore < 0 ||
                     testScore > 40)
            {
                resultRow.Errors.Add(
                    "Test score must be between 0 and 40.");
            }
            else
            {
                resultRow.TestScore =
                    testScore;
            }

            // --------------------------------------------------------
            // EXAM SCORE
            // --------------------------------------------------------

            if (!decimal.TryParse(
                    examText,
                    out var examScore))
            {
                resultRow.Errors.Add(
                    "Exam score must be a valid number.");
            }
            else if (examScore < 0 ||
                     examScore > 60)
            {
                resultRow.Errors.Add(
                    "Exam score must be between 0 and 60.");
            }
            else
            {
                resultRow.ExamScore =
                    examScore;
            }

            // --------------------------------------------------------
            // TOTAL
            // --------------------------------------------------------

            if (resultRow.TestScore.HasValue &&
                resultRow.ExamScore.HasValue)
            {
                resultRow.Score =
                    resultRow.TestScore.Value +
                    resultRow.ExamScore.Value;
            }

            // --------------------------------------------------------
            // DUPLICATE CHECK
            // --------------------------------------------------------

            if (students.TryGetValue(
                    studentNumber,
                    out var existingStudent))
            {
                var exists =
                    await _context.StudentResults
                        .AsNoTracking()
                        .AnyAsync(x =>
                            x.StudentId == existingStudent.Id &&
                            x.SubjectId == request.SubjectId &&
                            x.ClassId == request.ClassId &&
                            x.SchoolId == schoolId &&
                            x.Session == request.Session &&
                            x.Term == request.Term);

                if (exists)
                {
                    resultRow.Errors.Add(
                        "A result already exists for this student.");
                }
            }

            // --------------------------------------------------------
            // VALID
            // --------------------------------------------------------

            resultRow.IsValid =
                resultRow.Errors.Count == 0;

            preview.Rows.Add(resultRow);
        }

        // ------------------------------------------------------------
        // SUMMARY
        // ------------------------------------------------------------

        preview.TotalRows =
            preview.Rows.Count;

        preview.ValidRows =
            preview.Rows.Count(x =>
                x.IsValid);

        preview.InvalidRows =
            preview.Rows.Count(x =>
                !x.IsValid);

        preview.CanImport =
            preview.TotalRows > 0 &&
            preview.InvalidRows == 0;

        return (
            true,
            preview,
            null
        );
    }

    // ================================================================
    // CONFIRM EXCEL IMPORT
    // ================================================================

    public async Task<(
        bool Success,
        object? Data,
        string? Error
    )> ConfirmImportAsync(
        Guid schoolId,
        ConfirmResultImportRequest request)
    {
        // ------------------------------------------------------------
        // VALIDATE SCHOOL
        // ------------------------------------------------------------

        var school = await _context.Schools
            .FirstOrDefaultAsync(x =>
                x.Id == schoolId);

        if (school == null)
        {
            return (
                false,
                null,
                "School not found."
            );
        }

        // ------------------------------------------------------------
        // VALIDATE CLASS
        // ------------------------------------------------------------

        var classroom = await _context.Classes
            .FirstOrDefaultAsync(x =>
                x.Id == request.ClassId &&
                x.SchoolId == schoolId);

        if (classroom == null)
        {
            return (
                false,
                null,
                "Class does not belong to this school."
            );
        }

        // ------------------------------------------------------------
        // VALIDATE SUBJECT
        // ------------------------------------------------------------

        var subject = await _context.Subjects
            .FirstOrDefaultAsync(x =>
                x.Id == request.SubjectId &&
                x.SchoolId == schoolId);

        if (subject == null)
        {
            return (
                false,
                null,
                "Subject does not belong to this school."
            );
        }

        // ------------------------------------------------------------
        // VALIDATE REQUEST
        // ------------------------------------------------------------

        if (request.Rows == null ||
            request.Rows.Count == 0)
        {
            return (
                false,
                null,
                "There are no results to import."
            );
        }

        if (string.IsNullOrWhiteSpace(request.Session))
        {
            return (
                false,
                null,
                "Session is required."
            );
        }

        if (string.IsNullOrWhiteSpace(request.Term))
        {
            return (
                false,
                null,
                "Term is required."
            );
        }

        // ------------------------------------------------------------
        // PRELOAD STUDENTS
        // ------------------------------------------------------------

        var studentNumbers = request.Rows
            .Select(x => x.StudentNumber.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var students = await _context.StudentProfiles
            .Where(x =>
                x.SchoolId == schoolId &&
                x.ClassId == request.ClassId &&
                studentNumbers.Contains(x.StudentNumber))
            .ToDictionaryAsync(
                x => x.StudentNumber,
                x => x);

        // ------------------------------------------------------------
        // VALIDATE EXISTING RESULTS IN ONE QUERY
        // ------------------------------------------------------------

        var studentIds = students.Values
            .Select(x => x.Id)
            .ToList();

        var existingResults =
            await _context.StudentResults
                .AsNoTracking()
                .Where(x =>
                    x.SchoolId == schoolId &&
                    x.ClassId == request.ClassId &&
                    x.SubjectId == request.SubjectId &&
                    x.Session == request.Session.Trim() &&
                    x.Term == request.Term.Trim() &&
                    studentIds.Contains(x.StudentId))
                .Select(x => x.StudentId)
                .ToListAsync();

        var existingStudentIds =
            existingResults.ToHashSet();

        // ------------------------------------------------------------
        // BUILD RESULTS
        // ------------------------------------------------------------

        var results =
            new List<StudentResult>();

        foreach (var row in request.Rows)
        {
            var studentNumber =
                row.StudentNumber.Trim();

            // --------------------------------------------------------
            // STUDENT
            // --------------------------------------------------------

            if (!students.TryGetValue(
                    studentNumber,
                    out var student))
            {
                return (
                    false,
                    null,
                    $"Student '{studentNumber}' was not found in the selected class."
                );
            }

            // --------------------------------------------------------
            // SCORE
            // --------------------------------------------------------

            if (row.TestScore < 0 ||
                row.TestScore > 40)
            {
                return (
                    false,
                    null,
                    $"Invalid test score for student '{studentNumber}'."
                );
            }

            if (row.ExamScore < 0 ||
                row.ExamScore > 60)
            {
                return (
                    false,
                    null,
                    $"Invalid exam score for student '{studentNumber}'."
                );
            }

            // --------------------------------------------------------
            // DUPLICATE
            // --------------------------------------------------------

            if (existingStudentIds.Contains(student.Id))
            {
                return (
                    false,
                    null,
                    $"A result already exists for '{studentNumber}'."
                );
            }

            // --------------------------------------------------------
            // TOTAL
            // --------------------------------------------------------

            var total =
                row.TestScore +
                row.ExamScore;

            if (total < 0 ||
                total > 100)
            {
                return (
                    false,
                    null,
                    $"Total score for '{studentNumber}' must be between 0 and 100."
                );
            }

            // --------------------------------------------------------
            // GRADE
            // --------------------------------------------------------

            var grading =
                _gradingService.Calculate(total);

            // --------------------------------------------------------
            // CREATE ENTITY
            // --------------------------------------------------------

            results.Add(
                new StudentResult
                {
                    Id = Guid.NewGuid(),

                    StudentId = student.Id,

                    SchoolId = schoolId,

                    SubjectId = request.SubjectId,

                    ClassId = request.ClassId,

                    Session =
                        request.Session.Trim(),

                    Term =
                        request.Term.Trim(),

                    TestScore =
                        row.TestScore,

                    ExamScore =
                        row.ExamScore,

                    Score =
                        total,

                    Grade =
                        grading.Grade,

                    Remark =
                        string.IsNullOrWhiteSpace(row.Remark)
                            ? grading.Remark
                            : row.Remark.Trim(),

                    CreatedAt =
                        DateTime.UtcNow
                });
        }

        // ------------------------------------------------------------
        // SAVE TRANSACTION
        // ------------------------------------------------------------

        await using var transaction =
            await _context.Database
                .BeginTransactionAsync();

        try
        {
            await _context.StudentResults
                .AddRangeAsync(results);

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();

            return (
                false,
                null,
                "An error occurred while saving the results."
            );
        }

        // ------------------------------------------------------------
        // RESPONSE
        // ------------------------------------------------------------

        return (
            true,
            new
            {
                imported = results.Count,

                schoolId = school.Id,
                schoolName = school.Name,

                classId = classroom.Id,
                className = classroom.Name,

                subjectId = subject.Id,
                subjectName = subject.Name,

                session = request.Session.Trim(),
                term = request.Term.Trim()
            },
            null
        );
    }
}
