using System.Globalization;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Attendance;
using UserManagementApi.DTOs.Results;
using UserManagementApi.DTOs.TeacherPortal;
using UserManagementApi.Models;
using UserManagementApi.Models.Attendance;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;
public class TeacherPortalService(
    ApplicationDbContext context,
    ILogger<TeacherPortalService> logger) : ITeacherPortalService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<TeacherPortalService> _logger = logger;

    
/// <summary>
/// True when the teacher is assigned to teach this subject in this
/// class (TeacherSubjects row with that ClassId + SubjectId).
/// Being class teacher alone is NOT enough to upload a subject's
/// results — that belongs to the subject teacher.
/// </summary>
private Task<bool> TeachesSubjectInClassAsync(
    Guid teacherId,
    Guid classId,
    Guid subjectId)
{
    return _context.TeacherSubjects
        .AsNoTracking()
        .AnyAsync(x =>
            x.TeacherId == teacherId &&
            x.ClassId == classId &&
            x.SubjectId == subjectId);
}
 
/// <summary>
/// True when the teacher is the class teacher of this class.
/// Class teachers can VIEW every subject's results for their class.
/// </summary>
private async Task<bool> IsClassTeacherAsync(Guid teacherId, Guid classId)
{
    return
        await _context.TeacherClasses
            .AsNoTracking()
            .AnyAsync(x => x.TeacherId == teacherId && x.ClassId == classId)
        ||
        await _context.ClassTeachers
            .AsNoTracking()
            .AnyAsync(x => x.TeacherId == teacherId && x.ClassId == classId);
}
 

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
            _logger.LogWarning("Teacher profile not found for user ID: {UserId}", userId);
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
        .FirstOrDefaultAsync(x => x.UserId == userId);

    if (teacher == null)
    {
        return (false, null, "Teacher profile not found.");
    }

    // ------------------------------------------------------------
    // CLASSES THIS TEACHER CAN SEE
    // ------------------------------------------------------------
    // 1. Class teacher (TeacherClasses or ClassTeachers) — no
    //    subject in the class is needed.
    // 2. Subject teacher assigned to the class directly.
    // 3. Subject teacher with no class on the assignment, where the
    //    class offers that subject (ClassSubjects).

    var classTeacherClassIds = await _context.TeacherClasses
        .AsNoTracking()
        .Where(x =>
            x.TeacherId == teacher.Id &&
            x.Class.SchoolId == teacher.SchoolId)
        .Select(x => x.ClassId)
        .Union(
            _context.ClassTeachers
                .AsNoTracking()
                .Where(x =>
                    x.TeacherId == teacher.Id &&
                    x.Class.SchoolId == teacher.SchoolId)
                .Select(x => x.ClassId))
        .ToListAsync();

   var subjectClassIds = await _context.TeacherSubjects
    .AsNoTracking()
    .Where(x =>
        x.TeacherId == teacher.Id &&
        x.SchoolId == teacher.SchoolId)
    .Select(x => x.ClassId)
    .Distinct()
    .ToListAsync();

    var allowedClassIds = classTeacherClassIds
        .Union(subjectClassIds)
        .ToHashSet();

    // ------------------------------------------------------------
    // VERIFY CLASS FILTER
    // ------------------------------------------------------------

    if (classId.HasValue && !allowedClassIds.Contains(classId.Value))
    {
        return (
            false,
            null,
            "You are not the class teacher or a subject teacher of this class."
        );
    }

    var targetClassIds = classId.HasValue
        ? new List<Guid> { classId.Value }
        : allowedClassIds.ToList();

    // ------------------------------------------------------------
    // GET STUDENTS
    // ------------------------------------------------------------

    var students = await _context.StudentProfiles
        .AsNoTracking()
        .Where(student =>
            student.SchoolId == teacher.SchoolId &&
            targetClassIds.Contains(student.ClassId))
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

    // ------------------------------------------------------------
    // RESULT
    // ------------------------------------------------------------

    return (
        true,
        new
        {
            studentCount = students.Count,

            classId,

            // True when the teacher is the class teacher of the
            // filtered class (null when no class filter was given).
            isClassTeacher = classId.HasValue
                ? classTeacherClassIds.Contains(classId.Value)
                : (bool?)null,

            students
        },
        null
    );
}

   public async Task<(bool Success, object? Data, string? Error)>
    GetStudentDetailsAsync(
        string userId,
        Guid studentId)
{
    // ============================================================
    // GET TEACHER
    // ============================================================

    var teacher = await _context.Teachers
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.UserId == userId);

    if (teacher == null)
    {
        return (false, null, "Teacher profile not found.");
    }

    // ============================================================
    // GET STUDENT
    // ============================================================

    var student = await _context.StudentProfiles
        .AsNoTracking()
        .Where(x =>
            x.Id == studentId &&
            x.SchoolId == teacher.SchoolId)
        .Select(x => new
        {
            studentId = x.Id,
            studentName = x.User.FullName,
            studentNumber = x.StudentNumber,

            email = x.User.Email,
            phoneNumber = x.User.PhoneNumber,

            classId = x.ClassId,
            className = x.Class.Name,

            schoolId = x.SchoolId
        })
        .FirstOrDefaultAsync();

    if (student == null)
    {
        return (false, null, "Student not found.");
    }

    // ============================================================
    // TEACHER'S ROLE IN THIS CLASS
    // ============================================================
    // Access is allowed when the teacher is:
    //   • the class teacher of the student's class, and/or
    //   • a subject teacher in the student's class.

    var isClassTeacher =
        await _context.TeacherClasses
            .AsNoTracking()
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.ClassId == student.classId)
        ||
        await _context.ClassTeachers
            .AsNoTracking()
            .AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.ClassId == student.classId);

    var teacherClassSubjects = await _context.TeacherSubjects
        .AsNoTracking()
        .Where(x =>
            x.TeacherId == teacher.Id &&
            x.SchoolId == teacher.SchoolId &&
            x.ClassId == student.classId)
        .Select(x => new
        {
            x.SubjectId,
            subjectName = x.Subject.Name
        })
        .Distinct()
        .ToListAsync();

    if (!isClassTeacher && teacherClassSubjects.Count == 0)
    {
        return (
            false,
            null,
            "You are not the class teacher or a subject teacher of this student's class."
        );
    }

    var teacherSubjectIds = teacherClassSubjects
        .Select(x => x.SubjectId)
        .ToList();

    // ============================================================
    // RESULTS
    // ============================================================
    // Class teacher → every subject (they oversee the whole class).
    // Subject teacher only → just the subjects they teach.

    var results = await _context.StudentResults
        .AsNoTracking()
        .Where(result =>
            result.StudentId == studentId &&
            result.SchoolId == teacher.SchoolId &&
            (isClassTeacher || teacherSubjectIds.Contains(result.SubjectId)))
        .Select(result => new
        {
            ResultId = result.Id,
            SubjectId = result.SubjectId,
            SubjectName = result.Subject.Name,
            Session = result.Session,
            Term = result.Term,
            Score = result.Score,
            TestScore = result.TestScore,
            ExamScore = result.ExamScore,
            Grade = result.Grade,
            GradeRemark = result.GradeRemark,
            Remark = result.Remark,
            CreatedAt = result.CreatedAt
        })
        .OrderByDescending(x => x.CreatedAt)
        .ToListAsync();

    // ============================================================
    // ATTENDANCE
    // ============================================================
    // Two separate types:
    //   • Class attendance   (SubjectId == null) → class teacher only
    //   • Subject attendance (SubjectId set)     → subjects this
    //     teacher teaches in the class

    var attendance = await _context.AttendanceRecords
        .AsNoTracking()
        .Where(record =>
            record.StudentId == studentId &&
            record.SchoolId == teacher.SchoolId &&
            record.ClassId == student.classId &&
            (
                (isClassTeacher && record.SubjectId == null) ||
                (record.SubjectId != null &&
                 teacherSubjectIds.Contains(record.SubjectId.Value))
            ))
        .Select(record => new
        {
            attendanceId = record.Id,

            type = record.SubjectId == null ? "Class" : "Subject",

            subjectId = record.SubjectId,
            subjectName = record.SubjectId != null
                ? record.Subject!.Name
                : null,

            attendanceDate = record.AttendanceDate,
            status = record.Status,
            remarks = record.Remarks,

            session = record.Session,
            term = record.Term
        })
        .OrderByDescending(x => x.attendanceDate)
        .ToListAsync();

    // Rate = (Present + Late) / (Present + Absent + Late).
    // Excused and Holiday don't count for or against the student.
    object Summarise(IEnumerable<AttendanceStatus> statuses)
    {
        var list = statuses.ToList();

        var present = list.Count(s => s == AttendanceStatus.Present);
        var absent = list.Count(s => s == AttendanceStatus.Absent);
        var late = list.Count(s => s == AttendanceStatus.Late);
        var excused = list.Count(s => s == AttendanceStatus.Excused);
        var holiday = list.Count(s => s == AttendanceStatus.Holiday);

        var counted = present + absent + late;

        return new
        {
            total = list.Count,
            present,
            absent,
            late,
            excused,
            holiday,
            percentage = counted == 0
                ? (double?)null
                : Math.Round((double)(present + late) / counted * 100, 2)
        };
    }

    var classRecords = attendance.Where(x => x.subjectId == null).ToList();
    var subjectRecords = attendance.Where(x => x.subjectId != null).ToList();

    // ============================================================
    // ASSIGNMENTS
    // ============================================================
    // Class teacher → every assignment in the class.
    // Subject teacher only → their subjects.
    // Each one shows this student's submission, if any.

    var assignments = await _context.Assignments
        .AsNoTracking()
        .Where(assignment =>
            assignment.ClassId == student.classId &&
            assignment.SchoolId == teacher.SchoolId &&
            (isClassTeacher || teacherSubjectIds.Contains(assignment.SubjectId)))
        .Select(assignment => new
        {
            assignmentId = assignment.Id,

            subjectId = assignment.SubjectId,
            subjectName = assignment.Subject.Name,

            title = assignment.Title,
            description = assignment.Description,
            attachmentUrl = assignment.AttachmentUrl,

            dueDate = assignment.DueDate,
            isPublished = assignment.IsPublished,
            createdAt = assignment.CreatedAt,

            submission = assignment.Submissions
                .Where(s => s.StudentId == studentId)
                .Select(s => new
                {
                    s.Id,
                    s.SubmittedAt,
                    s.Score,
                    s.IsGraded
                })
                .FirstOrDefault()
        })
        .OrderByDescending(x => x.createdAt)
        .ToListAsync();

    // ============================================================
    // RESULT
    // ============================================================

    return (
        true,
        new
        {
            student,

            isClassTeacher,
            teacherSubjects = teacherClassSubjects,

            results,

            attendance = new
            {
                // Everything this teacher can see (both types).
                summary = Summarise(attendance.Select(x => x.status)),

                // Class teacher's daily register.
                classAttendance = isClassTeacher
                    ? Summarise(classRecords.Select(x => x.status))
                    : null,

                // This teacher's subject lessons.
                subjectAttendance = teacherSubjectIds.Count > 0
                    ? Summarise(subjectRecords.Select(x => x.status))
                    : null,

                records = attendance
            },

            assignments
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
        // // ============================================================
        // // VALIDATE CLASS
        // // ============================================================

        // var classExists = await _context.TeacherClasses
        //     .AsNoTracking()
        //     .AnyAsync(x =>
        //         x.TeacherId == teacher.Id &&
        //         x.ClassId == request.ClassId &&
        //         x.Class.SchoolId == schoolId);

        // if (!classExists)
        // {
        //     return (
        //         false,
        //         null,
        //         "You are not assigned to this class."
        //     );
        // }

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

        // // ============================================================
        // // TEACHER MUST TEACH SUBJECT
        // // ============================================================

        // var teachesSubject = await _context.TeacherSubjects
        //     .AsNoTracking()
        //     .AnyAsync(x =>
        //         x.TeacherId == teacher.Id &&
        //         x.SubjectId == request.SubjectId &&
        //         x.Subject.SchoolId == schoolId);

        // if (!teachesSubject)
        // {
        //     return (
        //         false,
        //         null,
        //         "You are not assigned to this subject."
        //     );
        // }

 if (!await TeachesSubjectInClassAsync(
               teacher.Id, request.ClassId, request.SubjectId))
       {
           return (
               false,
               null,
               "You don't teach this subject in this class."
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

        // var classExists = await _context.TeacherClasses
        //     .AsNoTracking()
        //     .AnyAsync(x =>
        //         x.TeacherId == teacher.Id &&
        //         x.ClassId == request.ClassId &&
        //         x.Class.SchoolId == schoolId);

        // if (!classExists)
        // {
        //     return (
        //         false,
        //         null,
        //         "You are not assigned to this class."
        //     );
        // }

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

        // // ============================================================
        // // TEACHER MUST TEACH SUBJECT
        // // ============================================================

        // var teachesSubject = await _context.TeacherSubjects
        //     .AsNoTracking()
        //     .AnyAsync(x =>
        //         x.TeacherId == teacher.Id &&
        //         x.SubjectId == request.SubjectId &&
        //         x.Subject.SchoolId == schoolId);

        // if (!teachesSubject)
        // {
        //     return (
        //         false,
        //         null,
        //         "You are not assigned to this subject."
        //     );
        // }

          if (!await TeachesSubjectInClassAsync(
               teacher.Id, request.ClassId, request.SubjectId))
       {
           return (
               false,
               null,
               "You don't teach this subject in this class."
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

        // var classExists = await _context.TeacherClasses
        //     .AsNoTracking()
        //     .AnyAsync(x =>
        //         x.TeacherId == teacher.Id &&
        //         x.ClassId == request.ClassId &&
        //         x.Class.SchoolId == schoolId);

        // if (!classExists)
        // {
        //     return (
        //         false,
        //         null,
        //         "You are not assigned to this class."
        //     );
        // }

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

        // // ============================================================
        // // TEACHER MUST TEACH SUBJECT
        // // ============================================================

        // var teachesSubject = await _context.TeacherSubjects
        //     .AsNoTracking()
        //     .AnyAsync(x =>
        //         x.TeacherId == teacher.Id &&
        //         x.SubjectId == request.SubjectId &&
        //         x.Subject.SchoolId == schoolId);

        // if (!teachesSubject)
        // {
        //     return (
        //         false,
        //         null,
        //         "You are not assigned to this subject."
        //     );
        // }

             if (!await TeachesSubjectInClassAsync(
               teacher.Id, request.ClassId, request.SubjectId))
       {
           return (
               false,
               null,
               "You don't teach this subject in this class."
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

        // // ============================================================
        // // TEACHER MUST TEACH CLASS
        // // ============================================================

        // var teachesClass = await _context.TeacherClasses
        //     .AsNoTracking()
        //     .AnyAsync(x =>
        //         x.TeacherId == teacher.Id &&
        //         x.ClassId == request.ClassId &&
        //         x.Class.SchoolId == schoolId);

        // if (!teachesClass)
        // {
        //     return (
        //         false,
        //         null,
        //         "You are not assigned to this class."
        //     );
        // }

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

        // // ============================================================
        // // TEACHER MUST TEACH SUBJECT
        // // ============================================================

        // var teachesSubject = await _context.TeacherSubjects
        //     .AsNoTracking()
        //     .AnyAsync(x =>
        //         x.TeacherId == teacher.Id &&
        //         x.SubjectId == request.SubjectId &&
        //         x.Subject.SchoolId == schoolId);

        // if (!teachesSubject)
        // {
        //     return (
        //         false,
        //         null,
        //         "You are not assigned to this subject."
        //     );
        // }

             if (!await TeachesSubjectInClassAsync(
               teacher.Id, request.ClassId, request.SubjectId))
       {
           return (
               false,
               null,
               "You don't teach this subject in this class."
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

        // // ============================================================
        // // TEACHER MUST TEACH CLASS
        // // ============================================================

        // var teachesClass = await _context.TeacherClasses
        //     .AsNoTracking()
        //     .AnyAsync(x =>
        //         x.TeacherId == teacher.Id &&
        //         x.ClassId == request.ClassId &&
        //         x.Class.SchoolId == schoolId);

        // if (!teachesClass)
        // {
        //     return (
        //         false,
        //         null,
        //         "You are not assigned to this class."
        //     );
        // }

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

        // // ============================================================
        // // TEACHER MUST TEACH SUBJECT
        // // ============================================================

        // var teachesSubject = await _context.TeacherSubjects
        //     .AsNoTracking()
        //     .AnyAsync(x =>
        //         x.TeacherId == teacher.Id &&
        //         x.SubjectId == request.SubjectId &&
        //         x.Subject.SchoolId == schoolId);

        // if (!teachesSubject)
        // {
        //     return (
        //         false,
        //         null,
        //         "You are not assigned to this subject."
        //     );
        // }

             if (!await TeachesSubjectInClassAsync(
               teacher.Id, request.ClassId, request.SubjectId))
       {
           return (
               false,
               null,
               "You don't teach this subject in this class."
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

        // var teachesClass = await _context.TeacherClasses
        //     .AsNoTracking()
        //     .AnyAsync(x =>
        //         x.TeacherId == teacher.Id &&
        //         x.ClassId == request.ClassId &&
        //         x.Class.SchoolId == schoolId);

        // if (!teachesClass)
        // {
        //     return (
        //         false,
        //         null,
        //         "You are not assigned to this class."
        //     );
        // }

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

        // // ============================================================
        // // TEACHER MUST TEACH SUBJECT
        // // ============================================================

        // var teachesSubject = await _context.TeacherSubjects
        //     .AsNoTracking()
        //     .AnyAsync(x =>
        //         x.TeacherId == teacher.Id &&
        //         x.SubjectId == request.SubjectId &&
        //         x.Subject.SchoolId == schoolId);

        // if (!teachesSubject)
        // {
        //     return (
        //         false,
        //         null,
        //         "You are not assigned to this subject."
        //     );
        // }

             if (!await TeachesSubjectInClassAsync(
               teacher.Id, request.ClassId, request.SubjectId))
       {
           return (
               false,
               null,
               "You don't teach this subject in this class."
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
        // TEACHER MUST TEACH THIS SUBJECT IN THIS CLASS
        // ============================================================

        if (!await TeachesSubjectInClassAsync(
                teacher.Id, result.ClassId, result.SubjectId))
        {
            return (
                false,
                null,
                "You don't teach this subject in this class."
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

        // // ============================================================
        // // TEACHER MUST TEACH CLASS
        // // ============================================================

        // var teachesClass = await _context.TeacherClasses
        //     .AnyAsync(x =>
        //         x.TeacherId == teacher.Id &&
        //         x.ClassId == result.ClassId &&
        //         x.Class.SchoolId == schoolId);

        // if (!teachesClass)
        // {
        //     return (
        //         false,
        //         null,
        //         "You are not assigned to this class."
        //     );
        // }

        // // ============================================================
        // // TEACHER MUST TEACH SUBJECT
        // // ============================================================

        // var teachesSubject = await _context.TeacherSubjects
        //     .AnyAsync(x =>
        //         x.TeacherId == teacher.Id &&
        //         x.SubjectId == result.SubjectId &&
        //         x.Subject.SchoolId == schoolId);

        // if (!teachesSubject)
        // {
        //     return (
        //         false,
        //         null,
        //         "You are not assigned to this subject."
        //     );
        // }

                // ============================================================
        // TEACHER MUST TEACH THIS SUBJECT IN THIS CLASS
        // ============================================================

        if (!await TeachesSubjectInClassAsync(
                teacher.Id, result.ClassId, result.SubjectId))
        {
            return (
                false,
                null,
                "You don't teach this subject in this class."
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

        // // ============================================================
        // // TEACHER MUST TEACH CLASS
        // // ============================================================

        // var teachesClass = await _context.TeacherClasses
        //     .AnyAsync(x =>
        //         x.TeacherId == teacher.Id &&
        //         x.ClassId == result.ClassId &&
        //         x.Class.SchoolId == schoolId);

        // if (!teachesClass)
        // {
        //     return (
        //         false,
        //         null,
        //         "You are not assigned to this class."
        //     );
        // }

        // // ============================================================
        // // TEACHER MUST TEACH SUBJECT
        // // ============================================================

        // var teachesSubject = await _context.TeacherSubjects
        //     .AnyAsync(x =>
        //         x.TeacherId == teacher.Id &&
        //         x.SubjectId == result.SubjectId &&
        //         x.Subject.SchoolId == schoolId);

        // if (!teachesSubject)
        // {
        //     return (
        //         false,
        //         null,
        //         "You are not assigned to this subject."
        //     );
        // }

        // ============================================================
        // TEACHER MUST TEACH THIS SUBJECT IN THIS CLASS
        // ============================================================

        if (!await TeachesSubjectInClassAsync(
                teacher.Id, result.ClassId, result.SubjectId))
        {
            return (
                false,
                null,
                "You don't teach this subject in this class."
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
    var teacher = await _context.Teachers
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.UserId == userId);
 
    if (teacher == null)
    {
        return (false, null, "Teacher profile not found.");
    }
 
    var schoolId = teacher.SchoolId;
    var teacherId = teacher.Id;
 
    // ------------------------------------------------------------
    // PERIOD (default to current)
    // ------------------------------------------------------------
 
    var session = request.Session?.Trim();
    var term = request.Term?.Trim();
 
    if (string.IsNullOrWhiteSpace(session) || string.IsNullOrWhiteSpace(term))
    {
        var current = await _context.AcademicSessions
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.IsCurrent)
            .Select(x => new
            {
                x.Session,
                Term = _context.AcademicTerms
                    .Where(t => t.AcademicSessionId == x.Id && t.IsCurrent)
                    .Select(t => t.Term)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();
 
        if (string.IsNullOrWhiteSpace(session)) session = current?.Session;
        if (string.IsNullOrWhiteSpace(term)) term = current?.Term;
    }
 
    // ------------------------------------------------------------
    // ACCESS
    // ------------------------------------------------------------
 
    var subjectClassPairs = _context.TeacherSubjects
        .Where(ts => ts.TeacherId == teacherId);
 
    var classTeacherClassIds = _context.TeacherClasses
        .Where(tc => tc.TeacherId == teacherId)
        .Select(tc => tc.ClassId)
        .Union(_context.ClassTeachers
            .Where(ct => ct.TeacherId == teacherId)
            .Select(ct => ct.ClassId));
 
    var query = _context.StudentResults
        .AsNoTracking()
        .Where(r =>
            r.SchoolId == schoolId &&
            r.Student.SchoolId == schoolId &&
            (
                subjectClassPairs.Any(ts =>
                    ts.ClassId == r.ClassId &&
                    ts.SubjectId == r.SubjectId) ||
                classTeacherClassIds.Contains(r.ClassId)
            ));
 
    // ------------------------------------------------------------
    // FILTERS
    // ------------------------------------------------------------
 
    if (request.StudentId.HasValue)
        query = query.Where(r => r.StudentId == request.StudentId.Value);
 
    if (request.ClassId.HasValue)
        query = query.Where(r => r.ClassId == request.ClassId.Value);
 
    if (request.SubjectId.HasValue)
        query = query.Where(r => r.SubjectId == request.SubjectId.Value);
 
    if (!string.IsNullOrWhiteSpace(session))
        query = query.Where(r => r.Session == session);
 
    if (!string.IsNullOrWhiteSpace(term))
        query = query.Where(r => r.Term == term);
 
    // ------------------------------------------------------------
    // RESULT
    // ------------------------------------------------------------
 
    var results = await query
        .OrderBy(r => r.Class.Name)
        .ThenBy(r => r.Student.User.FullName)
        .ThenBy(r => r.Subject.Name)
        .Select(r => new
        {
            resultId = r.Id,
 
            studentId = r.StudentId,
            studentName = r.Student.User.FullName,
            studentNumber = r.Student.StudentNumber,
 
            classId = r.ClassId,
            className = r.Class.Name,
 
            subjectId = r.SubjectId,
            subjectName = r.Subject.Name,
            subjectCode = r.Subject.Code,
 
            session = r.Session,
            term = r.Term,
 
            testScore = r.TestScore,
            examScore = r.ExamScore,
            score = r.Score,
 
            grade = r.Grade,
            remark = r.Remark,
 
            createdAt = r.CreatedAt
        })
        .ToListAsync();
 
    return (
        true,
        new
        {
            session,
            term,
            count = results.Count,
            results
        },
        null
    );
}

    // ================================================================
    // DELETE RESULT
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        DeleteResultAsync(
            string userId,
            Guid resultId)
    {
        // ------------------------------------------------------------
        // GET TEACHER
        // ------------------------------------------------------------

        var teacher = await _context.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (teacher == null)
        {
            return (false, null, "Teacher profile not found.");
        }

        var schoolId = teacher.SchoolId;

        // ------------------------------------------------------------
        // GET RESULT
        // ------------------------------------------------------------

        var result = await _context.StudentResults
            .FirstOrDefaultAsync(x =>
                x.Id == resultId &&
                x.SchoolId == schoolId);

        if (result == null)
        {
            // Keep this exact text: the controller returns 404 for it.
            return (false, null, "Result not found.");
        }

        // ------------------------------------------------------------
        // TEACHER MUST TEACH THIS SUBJECT IN THIS CLASS
        // ------------------------------------------------------------

        if (!await TeachesSubjectInClassAsync(
                teacher.Id, result.ClassId, result.SubjectId))
        {
            return (
                false,
                null,
                "You don't teach this subject in this class."
            );
        }

        // ------------------------------------------------------------
        // ONLY THE CURRENT TERM CAN BE DELETED
        // ------------------------------------------------------------

        var current = await _context.AcademicSessions
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.IsCurrent)
            .Select(x => new
            {
                x.Session,
                Term = _context.AcademicTerms
                    .Where(t => t.AcademicSessionId == x.Id && t.IsCurrent)
                    .Select(t => t.Term)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (current == null ||
            result.Session != current.Session ||
            result.Term != current.Term)
        {
            return (
                false,
                null,
                "Only results for the current academic period can be deleted."
            );
        }

        // ------------------------------------------------------------
        // DELETE
        // ------------------------------------------------------------

        var deleted = new
        {
            resultId = result.Id,
            studentId = result.StudentId,
            classId = result.ClassId,
            subjectId = result.SubjectId,
            session = result.Session,
            term = result.Term
        };

        _context.StudentResults.Remove(result);

        await _context.SaveChangesAsync();

        return (true, deleted, null);
    }

    public async Task<(
     bool Success,
     object? Data,
     string? Error
 )> CreateAssignmentAsync(
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
        // VALIDATE CLASS + SUBJECT + TEACHER ASSIGNMENT
        //
        // TeacherSubject contains:
        // TeacherId
        // SubjectId
        // ClassId
        //
        // Therefore this is the correct relationship to validate.
        // ============================================================

        var teacherSubject = await _context.TeacherSubjects
            .AsNoTracking()
            .Where(x =>
                x.TeacherId == teacher.Id &&
                x.SubjectId == request.SubjectId &&
                x.ClassId == request.ClassId &&
                x.SchoolId == schoolId)
            .Select(x => new
            {
                x.Id,
                x.TeacherId,
                x.SubjectId,
                x.ClassId,
                x.SchoolId,

                SubjectName = x.Subject.Name,
                ClassName = x.Class.Name
            })
            .FirstOrDefaultAsync();

        if (teacherSubject == null)
        {
            return (
                false,
                null,
                "You are not assigned to teach this subject for this class."
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
                className = teacherSubject.ClassName,

                subjectId = assignment.SubjectId,
                subjectName = teacherSubject.SubjectName,

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

    
public async Task<(bool Success, object? Data, string? Error)>
    GradeSubmissionAsync(
        string userId,
        Guid assignmentId,
        Guid submissionId,
        GradeAssignmentRequest request)
{
    var teacher = await _context.Teachers
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.UserId == userId);

    if (teacher == null)
    {
        return (false, null, "Teacher profile not found.");
    }

    var assignment = await _context.Assignments
        .AsNoTracking()
        .FirstOrDefaultAsync(x =>
            x.Id == assignmentId &&
            x.TeacherId == teacher.Id &&
            x.SchoolId == teacher.SchoolId);

    if (assignment == null)
    {
        return (false, null, "Assignment not found.");
    }

    if (request.Score < 0 || request.Score > 10)
    {
        return (false, null, "Score must be between 0 and 10.");
    }

    var submission = await _context.AssignmentSubmissions
        .FirstOrDefaultAsync(x =>
            x.Id == submissionId &&
            x.AssignmentId == assignmentId);

    if (submission == null)
    {
        return (false, null, "Submission not found for this assignment.");
    }

    submission.Score = request.Score;
    submission.Feedback = request.Feedback?.Trim();
    submission.IsGraded = true;
    submission.GradedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    return (
        true,
        new
        {
            submissionId = submission.Id,
            assignmentId = assignmentId,
            studentId = submission.StudentId,
            score = submission.Score,
            feedback = submission.Feedback,
            isGraded = submission.IsGraded,
            gradedAt = submission.GradedAt
        },
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
        // VERIFY TEACHER PERMISSION
        //
        // SubjectId == null
        //      => General/Class attendance
        //
        // SubjectId != null
        //      => Subject attendance
        // ============================================================

        if (request.SubjectId.HasValue)
        {
            // ========================================================
            // SUBJECT ATTENDANCE
            // ========================================================

            var teachesSubject = await _context.TeacherSubjects
                .AsNoTracking()
                .AnyAsync(x =>
                    x.TeacherId == teacher.Id &&
                    x.SubjectId == request.SubjectId.Value &&
                    x.ClassId == request.ClassId &&
                    x.SchoolId == schoolId);

            if (!teachesSubject)
            {
                return (
                    false,
                    null,
                    "You are not assigned to teach this subject in this class."
                );
            }
        }
        else
        {
            // ========================================================
            // GENERAL / CLASS ATTENDANCE
            //
            // Only the class teacher can mark this.
            // ========================================================

            var isClassTeacher = await _context.TeacherClasses
                .AsNoTracking()
                .AnyAsync(x =>
                    x.TeacherId == teacher.Id &&
                    x.ClassId == request.ClassId &&
                    x.Class.SchoolId == schoolId);

            if (!isClassTeacher)
            {
                return (
                    false,
                    null,
                    "You are not the class teacher for this class."
                );
            }
        }

        // ============================================================
        // GET STUDENT
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

        // ============================================================
        // VERIFY STUDENT BELONGS TO CLASS
        // ============================================================

        if (student.ClassId != request.ClassId)
        {
            return (
                false,
                null,
                "Student does not belong to this class."
            );
        }

        // ============================================================
        // ATTENDANCE DATE
        // ============================================================

        var date = request.AttendanceDate.Date;

        // ============================================================
        // DUPLICATE CHECK
        //
        // Class attendance:
        //     Student + Class + Date + Session + Term + null Subject
        //
        // Subject attendance:
        //     Student + Class + Subject + Date + Session + Term
        // ============================================================

        var duplicate = await _context.AttendanceRecords
            .AsNoTracking()
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
                request.SubjectId.HasValue
                    ? "Subject attendance has already been recorded for this student on this date."
                    : "Class attendance has already been recorded for this student on this date."
            );
        }

        // ============================================================
        // CREATE ATTENDANCE
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

        // ============================================================
        // RESPONSE
        // ============================================================

        return (
            true,
            new
            {
                attendanceId = attendance.Id,

                studentId = student.Id,

                studentName = student.studentName,

                studentNumber = student.StudentNumber,

                classId = attendance.ClassId,

                subjectId = attendance.SubjectId,

                attendanceDate = attendance.AttendanceDate,

                status = attendance.Status.ToString(),

                remarks = attendance.Remarks,

                session = attendance.Session,

                term = attendance.Term,

                teacherId = attendance.TeacherId,

                createdAt = attendance.CreatedAt,

                attendanceType = attendance.SubjectId.HasValue
                    ? "Subject"
                    : "Class"
            },
            null
        );
    }


    // ================================================================
    // GET ATTENDANCE
    // ================================================================

 public async Task<(
    bool Success,
    object? Data,
    string? Error
)> GetAttendanceAsync(
    string userId,
    GetTeacherAttendanceRequest request)
{
    try
    {
        // 1. Find the teacher using the logged-in user's ID
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

        // 2. Get attendance records belonging to this teacher
        // and the same school
        var query = _context.AttendanceRecords
            .AsNoTracking()
            .Where(x =>
                x.TeacherId == teacher.Id &&
                x.SchoolId == teacher.SchoolId);

        // 3. Filter by student
        if (request.StudentId.HasValue)
        {
            query = query.Where(x =>
                x.StudentId == request.StudentId.Value);
        }

        // 4. Filter by class
        if (request.ClassId.HasValue)
        {
            query = query.Where(x =>
                x.ClassId == request.ClassId.Value);
        }

        // 5. Filter by subject
        // Also include class-level records saved without a subject
        if (request.SubjectId.HasValue)
        {
            query = query.Where(x =>
                x.SubjectId == request.SubjectId.Value ||
                x.SubjectId == null);
        }

        // 6. Filter by date using a UTC range
        if (request.Date.HasValue)
        {
            var raw = request.Date.Value;

            // Normalise to UTC so Npgsql accepts it for timestamptz:
            // - "2026-10-26" / "2026-10-26T00:00:00" -> Unspecified -> treat as UTC
            // - "2026-10-26T00:00:00Z"                -> already UTC
            // - "2026-10-26T00:00:00+01:00"           -> Local -> keep the client's calendar day
            var utc = raw.Kind switch
            {
                DateTimeKind.Utc => raw,
                DateTimeKind.Local => DateTime.SpecifyKind(raw.Date, DateTimeKind.Utc),
                _ => DateTime.SpecifyKind(raw, DateTimeKind.Utc)
            };

            var date = DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
            var nextDate = date.AddDays(1);

            query = query.Where(x =>
                x.AttendanceDate >= date &&
                x.AttendanceDate < nextDate);
        }

        // 7. Filter by academic session
        if (!string.IsNullOrWhiteSpace(request.Session))
        {
            query = query.Where(x =>
                x.Session == request.Session);
        }

        // 8. Filter by academic term
        if (!string.IsNullOrWhiteSpace(request.Term))
        {
            query = query.Where(x =>
                x.Term == request.Term);
        }

        // 9. Retrieve and format attendance records
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

                remarks = x.Remarks,

                session = x.Session,
                term = x.Term
            })
            .ToListAsync();

        // 10. Return successful response
        return (
            true,
            records,
            null
        );
    }
    catch (Exception ex)
    {
        // 11. Log the full error for debugging
        _logger.LogError(
            ex,
            "Error retrieving attendance for teacher user {UserId}",
            userId);

        // 12. Return a failure response
        return (
            false,
            null,
            "An error occurred while retrieving attendance records."
        );
    }
}


public async Task<(bool Success, object? Data, string? Error)>
CreateBulkAttendanceAsync(
    string userId,
    CreateBulkAttendanceRequest request)
{
    try
    {
        // ============================================================
        // VALIDATE REQUEST
        // ============================================================

        if (request.Students == null || request.Students.Count == 0)
        {
            return (false, null, "No students were supplied for attendance.");
        }

        var hasDuplicates = request.Students
            .GroupBy(x => x.StudentId)
            .Any(x => x.Count() > 1);

        if (hasDuplicates)
        {
            return (false, null, "The attendance request contains duplicate students.");
        }

        var isSubjectAttendance = request.SubjectId.HasValue;

        // ============================================================
        // TEACHER
        // ============================================================

        var teacher = await _context.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (teacher == null)
        {
            return (false, null, "Teacher profile not found.");
        }

        var schoolId = teacher.SchoolId;

        // ============================================================
        // CURRENT ACADEMIC SESSION + TERM
        // ============================================================

        var period = await _context.AcademicSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.IsCurrent);

        if (period == null)
        {
            return (false, null, "There is no active academic session.");
        }

        var currentTerm = await _context.AcademicTerms
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AcademicSessionId == period.Id && x.IsCurrent);

        if (currentTerm == null)
        {
            return (false, null, "There is no active academic term.");
        }

        // ============================================================
        // CLASS
        // ============================================================

        var classExists = await _context.Classes
            .AsNoTracking()
            .AnyAsync(x => x.Id == request.ClassId && x.SchoolId == schoolId);

        if (!classExists)
        {
            return (false, null, "Class not found.");
        }

        // ============================================================
        // AUTHORIZATION
        // ============================================================
        //
        // Subject attendance -> teacher must be assigned the subject in
        //                       TeacherSubjects, either for this class or
        //                       school-wide (ClassId == null) where the
        //                       class offers the subject (ClassSubjects).
        //
        // Class attendance   -> teacher must be the class teacher.

        if (isSubjectAttendance)
        {
            var subjectId = request.SubjectId!.Value;

            var isAssigned = await _context.TeacherSubjects
                .AsNoTracking()
                .AnyAsync(x =>
                    x.TeacherId == teacher.Id &&
                    x.SubjectId == subjectId &&
                    (
                        x.ClassId == request.ClassId ||
                        (
                            x.ClassId == null &&
                            _context.ClassSubjects.Any(cs =>
                                cs.ClassId == request.ClassId &&
                                cs.SubjectId == subjectId)
                        )
                    ));

            if (!isAssigned)
            {
                return (
                    false,
                    null,
                    "You are not assigned to teach this subject in this class."
                );
            }
        }
        else
        {
            var isClassTeacher = await _context.TeacherClasses
                .AsNoTracking()
                .AnyAsync(x =>
                    x.TeacherId == teacher.Id &&
                    x.ClassId == request.ClassId &&
                    x.Class.SchoolId == schoolId);

            if (!isClassTeacher)
            {
                return (
                    false,
                    null,
                    "Only the class teacher can mark attendance for the whole class."
                );
            }
        }

        // ============================================================
        // DATE (always a UTC calendar day)
        // ============================================================
        //
        // "2026-10-26", "2026-10-26T00:00:00" and "2026-10-26T00:00:00Z"
        // all resolve to 2026-10-26 00:00 UTC.

        var raw = request.AttendanceDate;

        var utc = raw.Kind switch
        {
            DateTimeKind.Utc => raw,
            DateTimeKind.Local => DateTime.SpecifyKind(raw.Date, DateTimeKind.Utc),
            _ => DateTime.SpecifyKind(raw, DateTimeKind.Utc)
        };

        var date = DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
        var nextDate = date.AddDays(1);

        // ============================================================
        // STUDENTS MUST BELONG TO THIS CLASS
        // ============================================================

        var studentIds = request.Students.Select(x => x.StudentId).ToList();

        var students = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x =>
                studentIds.Contains(x.Id) &&
                x.SchoolId == schoolId &&
                x.ClassId == request.ClassId)
            .Select(x => new
            {
                x.Id,
                x.StudentNumber,
                StudentName = x.User.FullName
            })
            .ToListAsync();

        if (students.Count != studentIds.Count)
        {
            return (false, null, "One or more students do not belong to this class.");
        }

        var studentLookup = students.ToDictionary(x => x.Id);

        // ============================================================
        // EXISTING RECORDS FOR THIS CLASS / SUBJECT / DAY
        // ============================================================
        //
        // Existing records are UPDATED so the teacher can correct
        // attendance and save again.

        var existingRecords = await _context.AttendanceRecords
            .Where(x =>
                x.SchoolId == schoolId &&
                x.ClassId == request.ClassId &&
                x.SubjectId == request.SubjectId &&
                x.AttendanceDate >= date &&
                x.AttendanceDate < nextDate &&
                studentIds.Contains(x.StudentId))
            .ToListAsync();

        var existingByStudent = existingRecords
            .GroupBy(x => x.StudentId)
            .ToDictionary(g => g.Key, g => g.First());

        // ============================================================
        // CREATE OR UPDATE
        // ============================================================

        var savedRecords = new List<AttendanceRecord>();
        var created = 0;
        var updated = 0;

        foreach (var item in request.Students)
        {
            if (existingByStudent.TryGetValue(item.StudentId, out var existing))
            {
                existing.Status = item.Status;
                existing.Remarks = item.Remarks;
                existing.TeacherId = teacher.Id;

                // Uncomment if AttendanceRecord has UpdatedAt:
                // existing.UpdatedAt = DateTime.UtcNow;

                savedRecords.Add(existing);
                updated++;

                continue;
            }

            var attendance = new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                StudentId = item.StudentId,
                ClassId = request.ClassId,
                TeacherId = teacher.Id,

                // null = class attendance, value = subject attendance
                SubjectId = request.SubjectId,

                AttendanceDate = date,
                Status = item.Status,
                Remarks = item.Remarks,
                Session = period.Session,
                Term = currentTerm.Term,
                CreatedAt = DateTime.UtcNow
            };

            _context.AttendanceRecords.Add(attendance);

            savedRecords.Add(attendance);
            created++;
        }

        await _context.SaveChangesAsync();

        // ============================================================
        // RESPONSE
        // ============================================================

        var records = savedRecords
            .Select(attendance =>
            {
                var student = studentLookup[attendance.StudentId];

                return new
                {
                    attendanceId = attendance.Id,
                    studentId = student.Id,
                    studentName = student.StudentName,
                    studentNumber = student.StudentNumber,
                    classId = attendance.ClassId,
                    subjectId = attendance.SubjectId,
                    attendanceDate = attendance.AttendanceDate,
                    status = attendance.Status.ToString(),
                    remarks = attendance.Remarks,
                    session = attendance.Session,
                    term = attendance.Term,
                    teacherId = attendance.TeacherId,
                    createdAt = attendance.CreatedAt
                };
            })
            .ToList();

        return (
            true,
            new
            {
                classId = request.ClassId,
                subjectId = request.SubjectId,
                attendanceDate = date,
                session = period.Session,
                term = currentTerm.Term,
                total = records.Count,
                created,
                updated,
                records
            },
            null
        );
    }
    catch (Exception ex)
    {
        _logger.LogError(
            ex,
            "Error saving bulk attendance for teacher user {UserId}",
            userId);

        return (false, null, "An error occurred while saving attendance.");
    }
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