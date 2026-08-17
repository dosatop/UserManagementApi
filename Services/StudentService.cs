using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.Students;
using UserManagementApi.Models;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

public class StudentService : IStudentService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserManagementService _userManagementService;
    private readonly UserManager<User> _userManager;

    public StudentService(
        ApplicationDbContext context,
        IUserManagementService userManagementService,
        UserManager<User> userManager)
    {
        _context = context;
        _userManagementService = userManagementService;
        _userManager = userManager;
    }

    // ================================================================
    // CREATE
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        CreateStudentAsync(
            Guid schoolId,
            CreateStudentRequest request)
    {
        var school = await _context.Schools
            .FirstOrDefaultAsync(x => x.Id == schoolId);

        if (school == null)
        {
            return (
                false,
                null,
                "School not found."
            );
        }

        var classroom = await _context.Classes
            .FirstOrDefaultAsync(x =>
                x.Id == request.ClassRoomId &&
                x.SchoolId == schoolId);

        if (classroom == null)
        {
            return (
                false,
                null,
                "Class does not belong to this school."
            );
        }

        var studentNumberExists = await _context.StudentProfiles
            .AnyAsync(x =>
                x.SchoolId == schoolId &&
                x.StudentNumber == request.StudentNumber);

        if (studentNumberExists)
        {
            return (
                false,
                null,
                "A student with this student number already exists in this school."
            );
        }

        var (Success, User, Error) =
            await _userManagementService.CreateUserAsync(
                request.FullName,
                request.Email,
                request.Password,
                null,
                null,
                Roles.Student);

        if (!Success)
        {
            return (
                false,
                null,
                Error
            );
        }

        var user = User!;

        var studentProfile = new StudentProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SchoolId = schoolId,
            StudentNumber = request.StudentNumber,
            ClassId = request.ClassRoomId
        };

        _context.StudentProfiles.Add(studentProfile);

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                studentId = studentProfile.Id,
                userId = user.Id,
                fullName = user.FullName,
                email = user.Email,
                phoneNumber = user.PhoneNumber,
                studentNumber = studentProfile.StudentNumber,
                classId = classroom.Id,
                className = classroom.Name,
                schoolId = school.Id,
                schoolName = school.Name
            },
            null
        );
    }

    // ================================================================
    // GET ALL
    // ================================================================
public async Task<(bool Success, object? Data, string? Error)>
    GetStudentsByClassOrSubjectAsync(
        Guid schoolId,
        Guid? classId,
        Guid? subjectId)
{
    
    // Validate class if supplied
    if (classId.HasValue)
    {
        var classExists = await _context.Classes.AnyAsync(x =>
            x.Id == classId.Value &&
            x.SchoolId == schoolId);

        if (!classExists)
        {
            return (false, null, "Class not found in this school.");
        }
    }

    // Validate subject if supplied
    if (subjectId.HasValue)
    {
        var subjectExists = await _context.Subjects.AnyAsync(x =>
            x.Id == subjectId.Value &&
            x.SchoolId == schoolId);

        if (!subjectExists)
        {
            return (false, null, "Subject not found in this school.");
        }
    }

    // Start with ALL students in this school
    var query = _context.StudentProfiles
        .AsNoTracking()
        .Where(x => x.SchoolId == schoolId);

    // If class was supplied
    if (classId.HasValue)
    {
        query = query.Where(x =>
            x.ClassId == classId.Value);
    }

    // If subject was supplied
    if (subjectId.HasValue)
    {
        query = query.Where(student =>
            _context.TeacherSubjects.Any(ts =>
                ts.ClassId == student.ClassId &&
                ts.SubjectId == subjectId.Value
            ));
    }

    var students = await query
        .Select(x => new
        {
            StudentId = x.Id,
            StudentNumber = x.StudentNumber,
            StudentName = x.User.FullName,

            ClassId = x.ClassId,
            ClassName = x.Class.Name
        })
        .OrderBy(x => x.ClassName)
        .ThenBy(x => x.StudentName)
        .ToListAsync();

    return (
        true,
        new
        {
            ClassId = classId,
            SubjectId = subjectId,
            StudentCount = students.Count,
            Students = students
        },
        null
    );
}
    // ================================================================
    // GET BY ID
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetStudentByIdAsync(
            Guid schoolId,
            Guid studentId)
    {
        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x =>
                x.Id == studentId &&
                x.SchoolId == schoolId)
            .Select(x => new
            {
                studentId = x.Id,
                userId = x.UserId,

                studentNumber = x.StudentNumber,

                fullName = x.User.FullName,
                email = x.User.Email,
                phoneNumber = x.User.PhoneNumber,

                classId = x.ClassId,
                className = x.Class.Name,

                schoolId = x.SchoolId,
                schoolName = x.School.Name
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

        return (
            true,
            student,
            null
        );
    }

    // ================================================================
    // UPDATE
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        UpdateStudentAsync(
            Guid schoolId,
            Guid studentId,
            UpdateStudentRequest request)
    {
        var student = await _context.StudentProfiles
            .Include(x => x.User)
            .FirstOrDefaultAsync(x =>
                x.Id == studentId &&
                x.SchoolId == schoolId);

        if (student == null)
        {
            return (
                false,
                null,
                "Student not found."
            );
        }

        // ------------------------------------------------------------
        // Check class
        // ------------------------------------------------------------

        var classroom = await _context.Classes
            .FirstOrDefaultAsync(x =>
                x.Id == request.ClassRoomId &&
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
        // Check student number
        // ------------------------------------------------------------

        var studentNumberExists =
            await _context.StudentProfiles
                .AnyAsync(x =>
                    x.SchoolId == schoolId &&
                    x.StudentNumber == request.StudentNumber &&
                    x.Id != studentId);

        if (studentNumberExists)
        {
            return (
                false,
                null,
                "A student with this student number already exists in this school."
            );
        }

        // ------------------------------------------------------------
        // Check email
        // ------------------------------------------------------------

        var emailExists =
            await _context.Users
                .AnyAsync(x =>
                    x.Email == request.Email &&
                    x.Id != student.UserId);

        if (emailExists)
        {
            return (
                false,
                null,
                "A user with this email already exists."
            );
        }

        // ------------------------------------------------------------
        // Update User
        // ------------------------------------------------------------

        student.User.FullName = request.FullName;
        student.User.Email = request.Email;
        student.User.UserName = request.Email;
        student.User.PhoneNumber = request.PhoneNumber;

        // ------------------------------------------------------------
        // Update Student Profile
        // ------------------------------------------------------------

        student.StudentNumber = request.StudentNumber;
        student.ClassId = request.ClassRoomId;

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                studentId = student.Id,
                userId = student.UserId,

                fullName = student.User.FullName,
                email = student.User.Email,
                phoneNumber = student.User.PhoneNumber,

                studentNumber = student.StudentNumber,

                classId = classroom.Id,
                className = classroom.Name,

                schoolId = student.SchoolId
            },
            null
        );
    }

    // ================================================================
    // DELETE
    // ================================================================

    public async Task<(bool Success, string? Error)>
        DeleteStudentAsync(
            Guid schoolId,
            Guid studentId)
    {
        var student = await _context.StudentProfiles
            .Include(x => x.User)
            .FirstOrDefaultAsync(x =>
                x.Id == studentId &&
                x.SchoolId == schoolId);

        if (student == null)
        {
            return (
                false,
                "Student not found."
            );
        }

        var user = student.User;

        // Delete StudentProfile first
        _context.StudentProfiles.Remove(student);

        var deleteUserResult =
            await _userManager.DeleteAsync(user);

        if (!deleteUserResult.Succeeded)
        {
            var errors = string.Join(
                ", ",
                deleteUserResult.Errors.Select(x => x.Description));

            return (
                false,
                $"Failed to delete student user: {errors}"
            );
        }

        await _context.SaveChangesAsync();

        return (
            true,
            null
        );
    }

    public async Task<(bool Success, object? Data, string? Error)>
    GetStudentAttendanceAsync(
        Guid schoolId,
        Guid studentId,
        string session,
        string term)
    {
        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x =>
                x.Id == studentId &&
                x.SchoolId == schoolId)
            .Select(x => new
            {
                x.Id,
                x.StudentNumber,
                x.SchoolId,
                x.ClassId,
                StudentName = x.User.FullName,
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
                x.SchoolId == schoolId &&
                x.ClassId == student.ClassId &&
                x.Session == session &&
                x.Term == term)
            .OrderByDescending(x => x.AttendanceDate)
            .Select(x => new
            {
                x.Id,
                x.AttendanceDate,

                Status = x.Status.ToString(),

                x.Remarks,

                x.SubjectId,

                SubjectName = x.Subject != null
                    ? x.Subject.Name
                    : null,

                x.TeacherId,

                x.ClassId,
                x.SchoolId,

                x.Session,
                x.Term
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

        var attendanceDays = total - holiday;

        var attendancePercentage = attendanceDays == 0
            ? 0
            : Math.Round(
                (decimal)(present + late) /
                attendanceDays * 100,
                2);

        return (
            true,
            new
            {
                StudentId = student.Id,
                StudentNumber = student.StudentNumber,
                StudentName = student.StudentName,

                SchoolId = student.SchoolId,

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
                    AttendanceDays = attendanceDays,
                    AttendancePercentage = attendancePercentage
                },

                Records = attendance
            },
            null
        );
    }

    public async Task<(bool Success, object? Data, string? Error)>
        GetStudentAssignmentsAsync(
            Guid schoolId,
            Guid studentId,
            string session,
            string term)
    {
        var student = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x =>
                x.Id == studentId &&
                x.SchoolId == schoolId)
            .Select(x => new
            {
                x.Id,
                x.StudentNumber,
                x.SchoolId,
                x.ClassId,
                StudentName = x.User.FullName,
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
                x.SchoolId == schoolId &&
                x.Session == session &&
                x.Term == term)
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

                IsPublished = x.IsPublished,

                Submission = x.Submissions
                    .Where(s => s.StudentId == studentId)
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
                StudentName = student.StudentName,

                SchoolId = student.SchoolId,

                ClassId = student.ClassId,
                ClassName = student.ClassName,

                Session = session,
                Term = term,

                TotalAssignments = assignments.Count,

                Assignments = assignments
            },
            null
        );
    }
}