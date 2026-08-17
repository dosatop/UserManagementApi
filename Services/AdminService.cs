using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Admin;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.Services;

namespace UserManagementApi.Services;

public class AdminService(ApplicationDbContext context) : IAdminService
{
    private readonly ApplicationDbContext _context = context;

    // ================================================================
    // DASHBOARD
    // ================================================================

    public async Task<AdminDashboardDto?> GetDashboardAsync(Guid schoolId)
    {
        var school = await _context.Schools
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == schoolId);

        if (school == null)
        {
            return null;
        }

        var userCount = await _context.Users
            .CountAsync(x => x.SchoolId == schoolId);

        var adminCount = await (
            from user in _context.Users
            join userRole in _context.UserRoles
                on user.Id equals userRole.UserId
            join role in _context.Roles
                on userRole.RoleId equals role.Id
            where user.SchoolId == schoolId
                  && role.Name == Roles.Admin
            select user.Id
        ).CountAsync();

        var teacherCount = await _context.Teachers
            .CountAsync(x => x.SchoolId == schoolId);

        var studentCount = await _context.StudentProfiles
            .CountAsync(x => x.SchoolId == schoolId);

        var classCount = await _context.Classes
            .CountAsync(x => x.SchoolId == schoolId);

        var subjectCount = await _context.Subjects
            .CountAsync(x => x.SchoolId == schoolId);

        return new AdminDashboardDto
        {
            SchoolId = school.Id,
            SchoolName = school.Name,

            UserCount = userCount,
            AdminCount = adminCount,
            TeacherCount = teacherCount,
            StudentCount = studentCount,
            ClassCount = classCount,
            SubjectCount = subjectCount
        };
    }

    // ================================================================
    // TEACHERS
    // ================================================================

 public async Task<List<AdminTeacherDto>> GetTeachersAsync(Guid schoolId)
{
    return await _context.Teachers
        .AsNoTracking()
        .Where(x => x.SchoolId == schoolId)
        .Select(x => new AdminTeacherDto
        {
            TeacherId = x.Id,
            UserId = x.UserId,
            SchoolId = x.SchoolId,

            TeacherName = x.User.FullName,
            Email = x.User.Email,
            PhoneNumber = x.User.PhoneNumber,

            Subjects = x.TeacherSubjects
                .Select(ts => new AdminTeacherSubjectDto
                {
                    SubjectId = ts.SubjectId,
                    SubjectName = ts.Subject.Name,
                    Code = ts.Subject.Code,

                    ClassId = ts.ClassId,
                    ClassName = ts.Class.Name
                })
                .ToList()
        })
        .ToListAsync();
}

    // ================================================================
    // STUDENTS
    // ================================================================

    public async Task<List<AdminStudentDto>> GetStudentsAsync(Guid schoolId)
    {
        var school = await _context.Schools
            .AsNoTracking()
            .Where(x => x.Id == schoolId)
            .Select(x => new
            {
                x.Id,
                x.Name
            })
            .FirstOrDefaultAsync();

        if (school == null)
        {
            return [];
        }

        return await _context.StudentProfiles
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .Select(x => new AdminStudentDto
            {
                StudentId = x.Id,
                StudentNumber = x.StudentNumber,

                SchoolId = x.SchoolId,
                SchoolName = school.Name,

                StudentName = x.User.FullName,
                Email = x.User.Email,
                PhoneNumber = x.User.PhoneNumber,

                ClassId = x.ClassId,
                ClassName = x.Class.Name,

                Parents = x.Parents
                    .Select(ps => new AdminParentDto
                    {
                        ParentId = ps.Parent.ParentId,
                        ParentName = ps.Parent.User.FullName,
                        Email = ps.Parent.User.Email,
                        PhoneNumber = ps.Parent.User.PhoneNumber
                    })
                    .ToList()
            })
            .ToListAsync();
    }

    public async Task<AdminStudentDto?> GetStudentAsync(
    Guid schoolId,
    Guid studentId)
{
    var student = await _context.StudentProfiles
        .AsNoTracking()
        .Where(x =>
            x.Id == studentId &&
            x.SchoolId == schoolId)
        .Select(x => new AdminStudentDto
        {
            StudentId = x.Id,
            StudentNumber = x.StudentNumber,

            SchoolId = x.SchoolId,
            SchoolName = x.School.Name,

            StudentName = x.User.FullName,
            Email = x.User.Email,
            PhoneNumber = x.User.PhoneNumber,

            ClassId = x.ClassId,
            ClassName = x.Class.Name,

            Parents = x.Parents
                .Select(ps => new AdminParentDto
                {
                    ParentId = ps.Parent.ParentId,
                    ParentName = ps.Parent.User.FullName,
                    Email = ps.Parent.User.Email,
                    PhoneNumber = ps.Parent.User.PhoneNumber
                })
                .ToList()
        })
        .FirstOrDefaultAsync();

    if (student == null)
    {
        return null;
    }

    // ============================================================
    // SUBJECTS
    // ============================================================

    student.Subjects = await _context.Subjects
        .AsNoTracking()
        .Where(x => x.SchoolId == schoolId)
        .OrderBy(x => x.Name)
        .Select(x => new AdminStudentSubjectDto
        {
            SubjectId = x.Id,
            SubjectName = x.Name,
            Code = x.Code
        })
        .ToListAsync();

    // ============================================================
    // RESULTS
    // ============================================================

    student.Results = await _context.StudentResults
        .AsNoTracking()
        .Where(x =>
            x.StudentId == studentId &&
            x.SchoolId == schoolId)
        .OrderByDescending(x => x.Session)
        .ThenBy(x => x.Term)
        .Select(x => new AdminStudentResultDto
        {
            Id = x.Id,

            SubjectId = x.SubjectId,
            SubjectName = x.Subject.Name,

            TestScore = x.TestScore,
            ExamScore = x.ExamScore,
            Score = x.Score,

            Grade = x.Grade,
            Remark = x.Remark,

            Session = x.Session,
            Term = x.Term
        })
        .ToListAsync();

    // ============================================================
    // ATTENDANCE
    // ============================================================

    student.Attendance = await _context.AttendanceRecords
        .AsNoTracking()
        .Where(x =>
            x.StudentId == studentId &&
            x.SchoolId == schoolId)
        .OrderByDescending(x => x.AttendanceDate)
        .Select(x => new AdminStudentAttendanceDto
        {
            Id = x.Id,

            AttendanceDate = x.AttendanceDate,

            Status = x.Status.ToString(),

            Remarks = x.Remarks,

            SubjectId = x.SubjectId,

            SubjectName = x.Subject != null
                ? x.Subject.Name
                : null,

            TeacherId = x.TeacherId,

            TeacherName = x.Teacher != null
                ? x.Teacher.User.FullName
                : null,

            ClassId = x.ClassId,
            SchoolId = x.SchoolId,

            Session = x.Session,
            Term = x.Term
        })
        .ToListAsync();

    // ============================================================
    // ASSIGNMENTS
    // ============================================================

    student.Assignments = await _context.Assignments
        .AsNoTracking()
        .Where(x =>
            x.ClassId == student.ClassId &&
            x.SchoolId == schoolId)
        .OrderByDescending(x => x.AssignedAt)
        .Select(x => new AdminStudentAssignmentDto
        {
            AssignmentId = x.Id,

            Title = x.Title,
            Description = x.Description,
            AttachmentUrl = x.AttachmentUrl,

            AssignedAt = x.AssignedAt,
            DueDate = x.DueDate,

            Session = x.Session,
            Term = x.Term,

            ClassId = x.ClassId,
            ClassName = x.Class.Name,

            SubjectId = x.SubjectId,
            SubjectName = x.Subject.Name,

            TeacherId = x.TeacherId,
            TeacherName = x.Teacher.User.FullName,

            IsPublished = x.IsPublished,

            Submission = x.Submissions
                .Where(s => s.StudentId == studentId)
                .Select(s => new AdminAssignmentSubmissionDto
                {
                    Id = s.Id,

                    SubmissionText = s.SubmissionText,
                    AttachmentUrl = s.AttachmentUrl,

                    SubmittedAt = s.SubmittedAt,

                    Score = s.Score,
                    Feedback = s.Feedback,

                    IsGraded = s.IsGraded,
                    GradedAt = s.GradedAt
                })
                .FirstOrDefault()
        })
        .ToListAsync();

    return student;
}

    // ================================================================
    // CLASSES
    // ================================================================

public async Task<List<AdminClassDto>> GetClassesAsync(Guid schoolId)
{
    return await _context.Classes
        .AsNoTracking()
        .Where(x => x.SchoolId == schoolId)
        .Select(x => new AdminClassDto
        {
            ClassId = x.Id,
            ClassName = x.Name,
            SchoolId = x.SchoolId,

            // ========================================================
            // STUDENTS
            // ========================================================

            Students = x.Students
                .Select(s => new AdminClassStudentDto
                {
                    StudentId = s.Id,
                    StudentNumber = s.StudentNumber,
                    StudentName = s.User.FullName,
                    Email = s.User.Email,
                    PhoneNumber = s.User.PhoneNumber
                })
                .ToList(),

            // ========================================================
            // TEACHERS + SUBJECTS
            // ========================================================

            Teachers = x.TeacherSubjects
                .Select(ts => new AdminClassTeacherDto
                {
                    TeacherId = ts.TeacherId,

                    TeacherName = ts.Teacher.User.FullName,
                    Email = ts.Teacher.User.Email,
                    PhoneNumber = ts.Teacher.User.PhoneNumber,

                    SubjectId = ts.SubjectId,
                    SubjectName = ts.Subject.Name,
                    SubjectCode = ts.Subject.Code
                })
                .ToList()
        })
        .ToListAsync();
}

    // ================================================================
    // SUBJECTS
    // ================================================================

    public async Task<List<AdminSubjectDto>> GetSubjectsAsync(Guid schoolId)
    {
        return await _context.Subjects
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .Select(x => new AdminSubjectDto
            {
                SubjectId = x.Id,
                SchoolId = x.SchoolId,

                SubjectName = x.Name,
                Code = x.Code,

                TeacherSubjects = x.TeacherSubjects
                    .Select(ts => new AdminSubjectTeacherDto
                    {
                        TeacherId = ts.TeacherId,
                        TeacherName = ts.Teacher.User.FullName,
                        Email = ts.Teacher.User.Email,
                        PhoneNumber = ts.Teacher.User.PhoneNumber
                    })
                    .ToList()
            })
            .ToListAsync();
    }
}