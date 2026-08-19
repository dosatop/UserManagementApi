using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.Teachers;
using UserManagementApi.Models;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

public class TeacherService(
    ApplicationDbContext context,
    IUserManagementService userManagementService,
    UserManager<User> userManager) : ITeacherService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IUserManagementService _userManagementService =
        userManagementService;
    private readonly UserManager<User> _userManager = userManager;


    // ================================================================
    // CREATE TEACHER
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        CreateTeacherAsync(
            Guid schoolId,
            CreateTeacherRequest request)
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

        var employeeExists = await _context.Teachers
            .AnyAsync(x =>
                x.SchoolId == schoolId &&
                x.EmployeeNumber == request.EmployeeNumber);

        if (employeeExists)
        {
            return (
                false,
                null,
                "A teacher with this employee number already exists in this school."
            );
        }

        var userResult =
            await _userManagementService.CreateUserAsync(
                request.FullName,
                request.Email,
                request.Password,
                request.PhoneNumber,
                request.EmployeeNumber,
                Roles.Teacher);

        if (!userResult.Success)
        {
            return (
                false,
                null,
                userResult.Error
            );
        }

        var user = userResult.User!;

        var teacherProfile = new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SchoolId = schoolId,
            PhoneNumber = request.PhoneNumber,
            EmployeeNumber = request.EmployeeNumber
        };

        _context.Teachers.Add(teacherProfile);

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                teacherId = teacherProfile.Id,
                userId = user.Id,
                fullName = user.FullName,
                email = user.Email,
                phoneNumber = user.PhoneNumber,
                employeeNumber = teacherProfile.EmployeeNumber,
                schoolId = school.Id,
                schoolName = school.Name
            },
            null
        );
    }


    // ================================================================
    // GET ALL TEACHERS
    // ================================================================

    public async Task<IEnumerable<object>> GetTeachersAsync(
        Guid schoolId)
    {
        return await _context.Teachers
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .Select(x => new
            {
                teacherId = x.Id,
                userId = x.UserId,
                employeeNumber = x.EmployeeNumber,

                fullName = x.User.FullName,
                email = x.User.Email,
                phoneNumber = x.User.PhoneNumber,

                schoolId = x.SchoolId,
                schoolName = x.School.Name
            })
            .ToListAsync();
    }


    // ================================================================
    // GET TEACHER BY ID
    // ================================================================
    public async Task<(bool Success, object? Data, string? Error)>
        GetTeacherByIdAsync(
            Guid schoolId,
            Guid teacherId)
    {
        var teacher = await _context.Teachers
            .AsNoTracking()
            .Where(x =>
                x.Id == teacherId &&
                x.SchoolId == schoolId)
            .Select(x => new
            {
                teacherId = x.Id,
                userId = x.UserId,

                employeeNumber = x.EmployeeNumber,

                fullName = x.User.FullName,
                email = x.User.Email,
                phoneNumber = x.User.PhoneNumber,

                schoolId = x.SchoolId,
                schoolName = x.School.Name,

                // Class teacher information
                isClassTeacher = x.TeacherClasses.Any(),

                classTeacher = x.TeacherClasses
                    .Select(tc => new
                    {
                        classId = tc.ClassId,
                        className = tc.Class.Name
                    })
                    .FirstOrDefault(),

                // Subjects taught
                subjects = x.TeacherSubjects
                    .Select(ts => new
                    {
                        subjectId = ts.SubjectId,
                        subjectTaught = ts.Subject.Name,
                        code = ts.Subject.Code,

                        classId = ts.ClassId,
                        className = ts.Class.Name
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (teacher == null)
        {
            return (
                false,
                null,
                "Teacher not found."
            );
        }

        return (
            true,
            teacher,
            null
        );
    }


    // ================================================================
    // UPDATE TEACHER
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        UpdateTeacherAsync(
            Guid schoolId,
            Guid teacherId,
            UpdateTeacherRequest request)
    {
        var teacher = await _context.Teachers
            .Include(x => x.User)
            .FirstOrDefaultAsync(x =>
                x.Id == teacherId &&
                x.SchoolId == schoolId);

        if (teacher == null)
        {
            return (
                false,
                null,
                "Teacher not found."
            );
        }

        // ------------------------------------------------------------
        // CHECK EMPLOYEE NUMBER
        // ------------------------------------------------------------

        var employeeExists = await _context.Teachers
            .AnyAsync(x =>
                x.SchoolId == schoolId &&
                x.EmployeeNumber == request.EmployeeNumber &&
                x.Id != teacherId);

        if (employeeExists)
        {
            return (
                false,
                null,
                "Another teacher with this employee number already exists in this school."
            );
        }

        // ------------------------------------------------------------
        // CHECK EMAIL
        // ------------------------------------------------------------

        var existingUser = await _userManager
            .FindByEmailAsync(request.Email);

        if (existingUser != null &&
            existingUser.Id != teacher.UserId)
        {
            return (
                false,
                null,
                "Another user already has this email address."
            );
        }

        // ------------------------------------------------------------
        // UPDATE USER DETAILS
        // ------------------------------------------------------------

        teacher.User.FullName = request.FullName;
        teacher.User.PhoneNumber = request.PhoneNumber;

        // ------------------------------------------------------------
        // UPDATE EMAIL
        // ------------------------------------------------------------

        if (!string.Equals(
                teacher.User.Email,
                request.Email,
                StringComparison.OrdinalIgnoreCase))
        {
            var emailResult = await _userManager
                .SetEmailAsync(
                    teacher.User,
                    request.Email);

            if (!emailResult.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    emailResult.Errors.Select(x => x.Description));

                return (
                    false,
                    null,
                    errors
                );
            }

            var usernameResult = await _userManager
                .SetUserNameAsync(
                    teacher.User,
                    request.Email);

            if (!usernameResult.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    usernameResult.Errors.Select(x => x.Description));

                return (
                    false,
                    null,
                    errors
                );
            }
        }

        // ------------------------------------------------------------
        // UPDATE TEACHER PROFILE
        // ------------------------------------------------------------

        teacher.EmployeeNumber = request.EmployeeNumber;
        teacher.PhoneNumber = request.PhoneNumber;

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                teacherId = teacher.Id,
                userId = teacher.UserId,
                fullName = teacher.User.FullName,
                email = teacher.User.Email,
                phoneNumber = teacher.User.PhoneNumber,
                employeeNumber = teacher.EmployeeNumber,
                schoolId = teacher.SchoolId
            },
            null
        );
    }


    // ================================================================
    // DELETE TEACHER
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        DeleteTeacherAsync(
            Guid schoolId,
            Guid teacherId)
    {
        var teacher = await _context.Teachers
            .FirstOrDefaultAsync(x =>
                x.Id == teacherId &&
                x.SchoolId == schoolId);

        if (teacher == null)
        {
            return (
                false,
                null,
                "Teacher not found."
            );
        }

        var user = await _userManager
            .FindByIdAsync(teacher.UserId.ToString());

        if (user == null)
        {
            return (
                false,
                null,
                "Teacher user account not found."
            );
        }

        // ------------------------------------------------------------
        // DELETE IDENTITY USER FIRST
        // ------------------------------------------------------------

        var userResult = await _userManager
            .DeleteAsync(user);

        if (!userResult.Succeeded)
        {
            var errors = string.Join(
                ", ",
                userResult.Errors.Select(x => x.Description));

            return (
                false,
                null,
                errors
            );
        }

        // ------------------------------------------------------------
        // DELETE TEACHER PROFILE
        // ------------------------------------------------------------

        _context.Teachers.Remove(teacher);

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                teacherId,
                message = "Teacher deleted successfully."
            },
            null
        );
    }


    // ================================================================
    // ASSIGN TEACHING SUBJECT
    // ================================================================
    //
    // A teaching subject assignment is:
    //
    // Teacher + Subject + Class
    //
    // ClassId is compulsory.
    //
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
     AssignTeachingSubjectAsync(
         Guid schoolId,
         Guid teacherId,
         AssignTeachingSubjectRequest request)
    {
        // ------------------------------------------------------------
        // CLASS VALIDATION
        // ------------------------------------------------------------

        if (request.ClassId == null || request.ClassId == Guid.Empty)
        {
            return (
                false,
                null,
                "Class is required when assigning a teaching subject."
            );
        }

        // ------------------------------------------------------------
        // TEACHER
        // ------------------------------------------------------------

        var teacher = await _context.Teachers
            .Include(x => x.User)
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

        // ------------------------------------------------------------
        // SUBJECT
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
                "Subject not found in this school."
            );
        }

        // ------------------------------------------------------------
        // CLASS
        // ------------------------------------------------------------

        if (!request.ClassId.HasValue)
        {
            return (
                false,
                null,
                "Class is required when assigning a teaching subject."
            );
        }

        var classroom = await _context.Classes
            .FirstOrDefaultAsync(x =>
                x.Id == request.ClassId.Value &&
                x.SchoolId == schoolId);

        if (classroom == null)
        {
            return (
                false,
                null,
                "Class not found in this school."
            );
        }

        // ------------------------------------------------------------
        // CHECK DUPLICATE ASSIGNMENT
        // ------------------------------------------------------------

        var alreadyAssigned = await _context.TeacherSubjects
            .AnyAsync(x =>
                x.TeacherId == teacherId &&
                x.SubjectId == request.SubjectId &&
                x.ClassId == request.ClassId.Value);

        if (alreadyAssigned)
        {
            return (
                false,
                null,
                "Teacher is already assigned to this subject for this class."
            );
        }

        // ------------------------------------------------------------
        // CREATE ASSIGNMENT
        // ------------------------------------------------------------

        var assignment = new TeacherSubject
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            SubjectId = request.SubjectId,
            ClassId = request.ClassId.Value
        };

        _context.TeacherSubjects.Add(assignment);

        await _context.SaveChangesAsync();

        // ------------------------------------------------------------
        // RESPONSE
        // ------------------------------------------------------------

        return (
            true,
            new
            {
                assignmentId = assignment.Id,

                teacherId = teacher.Id,
                teacherName = teacher.User.FullName,

                subjectId = subject.Id,
                subjectName = subject.Name,

                classId = classroom.Id,
                className = classroom.Name,

                message =
                    "Teacher assigned to subject and class successfully."
            },
            null
        );
    }


    // ================================================================
    // REMOVE TEACHING SUBJECT
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        RemoveTeachingSubjectAsync(
            Guid schoolId,
            Guid assignmentId)
    {
        var assignment = await _context.TeacherSubjects
            .Include(x => x.Teacher)
                .ThenInclude(x => x.User)
            .Include(x => x.Subject)
            .Include(x => x.Class)
            .FirstOrDefaultAsync(x =>
                x.Id == assignmentId &&
                x.Teacher.SchoolId == schoolId);

        if (assignment == null)
        {
            return (
                false,
                null,
                "Teaching subject assignment not found."
            );
        }

        // ------------------------------------------------------------
        // SAVE RESPONSE DETAILS
        // ------------------------------------------------------------

        var teacherId = assignment.TeacherId;
        var teacherName = assignment.Teacher.User.FullName;

        var subjectId = assignment.SubjectId;
        var subjectName = assignment.Subject.Name;

        var classId = assignment.ClassId;
        var className = assignment.Class.Name;

        // ------------------------------------------------------------
        // REMOVE ASSIGNMENT
        // ------------------------------------------------------------

        _context.TeacherSubjects.Remove(assignment);

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                assignmentId,

                teacherId,
                teacherName,

                subjectId,
                subjectName,

                classId,
                className,

                message =
                    "Teacher subject assignment removed successfully."
            },
            null
        );
    }


    // ================================================================
    // ASSIGN CLASS TEACHER
    // ================================================================
    //
    // A class teacher assignment is:
    //
    // Teacher + Class
    //
    // One class can have only one class teacher.
    //
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        AssignClassTeacherAsync(
            Guid schoolId,
            Guid teacherId,
            Guid classId)
    {
        // ------------------------------------------------------------
        // TEACHER
        // ------------------------------------------------------------

        var teacher = await _context.Teachers
            .Include(x => x.User)
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

        // ------------------------------------------------------------
        // CLASS
        // ------------------------------------------------------------

        var classroom = await _context.Classes
            .FirstOrDefaultAsync(x =>
                x.Id == classId &&
                x.SchoolId == schoolId);

        if (classroom == null)
        {
            return (
                false,
                null,
                "Class not found in this school."
            );
        }

        // ------------------------------------------------------------
        // CHECK IF CLASS ALREADY HAS A CLASS TEACHER
        // ------------------------------------------------------------

        var existingClassTeacher = await _context.TeacherClasses
            .FirstOrDefaultAsync(x =>
                x.ClassId == classId);

        if (existingClassTeacher != null)
        {
            return (
                false,
                null,
                "This class already has a class teacher."
            );
        }

        // ------------------------------------------------------------
        // CREATE ASSIGNMENT
        // ------------------------------------------------------------

        var assignment = new TeacherClass
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            ClassId = classId
        };

        _context.TeacherClasses.Add(assignment);

        await _context.SaveChangesAsync();

        // ------------------------------------------------------------
        // RESPONSE
        // ------------------------------------------------------------

        return (
            true,
            new
            {
                assignmentId = assignment.Id,

                teacherId = teacher.Id,
                teacherName = teacher.User.FullName,

                classId = classroom.Id,
                className = classroom.Name,

                message =
                    "Class teacher assigned successfully."
            },
            null
        );
    }


    // ================================================================
    // REMOVE CLASS TEACHER
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        RemoveClassTeacherAsync(
            Guid schoolId,
            Guid assignmentId)
    {
        var assignment = await _context.TeacherClasses
            .Include(x => x.Teacher)
                .ThenInclude(x => x.User)
            .Include(x => x.Class)
            .FirstOrDefaultAsync(x =>
                x.Id == assignmentId &&
                x.Teacher.SchoolId == schoolId);

        if (assignment == null)
        {
            return (
                false,
                null,
                "Class teacher assignment not found."
            );
        }

        // ------------------------------------------------------------
        // SAVE RESPONSE DETAILS
        // ------------------------------------------------------------

        var teacherId = assignment.TeacherId;
        var teacherName = assignment.Teacher.User.FullName;

        var classId = assignment.ClassId;
        var className = assignment.Class.Name;

        // ------------------------------------------------------------
        // REMOVE ASSIGNMENT
        // ------------------------------------------------------------

        _context.TeacherClasses.Remove(assignment);

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                assignmentId,

                teacherId,
                teacherName,

                classId,
                className,

                message =
                    "Class teacher assignment removed successfully."
            },
            null
        );
    }
}