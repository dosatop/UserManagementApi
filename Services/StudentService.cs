using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.Students;
using UserManagementApi.Models;
using UserManagementApi.Models.SchoolModels;
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
        // ============================================================
        // SCHOOL
        // ============================================================

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

        // ============================================================
        // CLASS
        // ============================================================

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

        // ============================================================
        // DEPARTMENT
        // ============================================================

        Department? department = null;

        if (request.DepartmentId.HasValue)
        {
            department = await _context.Departments
                .FirstOrDefaultAsync(x =>
                    x.Id == request.DepartmentId.Value &&
                    x.SchoolId == schoolId);

            if (department == null)
            {
                return (
                    false,
                    null,
                    "Department does not belong to this school."
                );
            }
        }

        // ============================================================
        // VALIDATE DEPARTMENT BASED ON SCHOOL LEVEL
        // ============================================================

        // Senior students MUST have a department
        if (classroom.Level == SchoolLevel.Senior &&
            !request.DepartmentId.HasValue)
        {
            return (
                false,
                null,
                "A department is required for Senior students."
            );
        }

        // Junior students CANNOT have a department
        if (classroom.Level == SchoolLevel.Junior &&
            request.DepartmentId.HasValue)
        {
            return (
                false,
                null,
                "Junior students cannot be assigned to a department."
            );
        }

        // ============================================================
        // TRADE
        // ============================================================

        // Trade is optional for BOTH Junior and Senior students.
        // Do NOT assign a trade during student creation.
        //
        // The student can choose a trade later through a separate
        // trade-assignment operation.

        // ============================================================
        // STUDENT NUMBER
        // ============================================================

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

        // ============================================================
        // CREATE USER
        // ============================================================

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

        // ============================================================
        // CREATE STUDENT PROFILE
        // ============================================================

        var studentProfile = new StudentProfile
        {
            Id = Guid.NewGuid(),

            UserId = user.Id,

            SchoolId = schoolId,

            StudentNumber = request.StudentNumber,

            ClassId = request.ClassRoomId,

            // Get level directly from the class
            SchoolLevel = classroom.Level,

            // Senior = required
            // Junior = null
            DepartmentId = department?.Id,

            // Trade is optional and NOT selected during creation
            TradeId = null
        };

        _context.StudentProfiles.Add(studentProfile);

        await _context.SaveChangesAsync();

        // ============================================================
        // RESPONSE
        // ============================================================

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

                schoolLevel = classroom.Level.ToString(),
                schoolLevelId = (int)classroom.Level,

                departmentId = department?.Id,
                departmentName = department?.Name,

                // No trade during creation
                tradeId = (Guid?)null,
                tradeName = (string?)null,

                schoolId = school.Id,
                schoolName = school.Name
            },
            null
        );
    }


    // ================================================================
    // GET ALL STUDENTS BY CLASS OR SUBJECT
    // ================================================================
    public async Task<(bool Success, object? Data, string? Error)>
        GetStudentsByClassOrSubjectAsync(
            Guid schoolId,
            Guid? classId,
            Guid? subjectId)
    {
        // ------------------------------------------------------------
        // VALIDATE CLASS
        // ------------------------------------------------------------

        if (classId.HasValue)
        {
            var classExists = await _context.Classes
                .AnyAsync(x =>
                    x.Id == classId.Value &&
                    x.SchoolId == schoolId);

            if (!classExists)
            {
                return (
                    false,
                    null,
                    "Class not found in this school."
                );
            }
        }

        // ------------------------------------------------------------
        // VALIDATE SUBJECT
        // ------------------------------------------------------------

        if (subjectId.HasValue)
        {
            var subjectExists = await _context.Subjects
                .AnyAsync(x =>
                    x.Id == subjectId.Value &&
                    x.SchoolId == schoolId);

            if (!subjectExists)
            {
                return (
                    false,
                    null,
                    "Subject not found in this school."
                );
            }
        }

        // ------------------------------------------------------------
        // START WITH ALL STUDENTS IN THIS SCHOOL
        // ------------------------------------------------------------

        var query = _context.StudentProfiles
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId);

        // ------------------------------------------------------------
        // FILTER BY CLASS
        // ------------------------------------------------------------

        if (classId.HasValue)
        {
            query = query.Where(x =>
                x.ClassId == classId.Value);
        }

        // ------------------------------------------------------------
        // FILTER BY SUBJECT
        // ------------------------------------------------------------
        // IMPORTANT:
        // ClassSubject determines whether students in a class
        // take a particular subject.
        //
        // TeacherSubject is NOT used here.
        // ------------------------------------------------------------

        if (subjectId.HasValue)
        {
            var subject = await _context.Subjects
                .AsNoTracking()
                .Where(x =>
                    x.Id == subjectId.Value &&
                    x.SchoolId == schoolId)
                .Select(x => new
                {
                    x.Id,
                    x.Type,
                    x.DepartmentId,
                    x.TradeId
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

            // ------------------------------------------------------------
            // GENERAL SUBJECT
            // ------------------------------------------------------------
            // Every student in the class can offer it.
            // ------------------------------------------------------------

            if (subject.Type == SubjectType.General)
            {
                query = query.Where(student =>
                    _context.ClassSubjects.Any(cs =>
                        cs.ClassId == student.ClassId &&
                        cs.SubjectId == subject.Id
                    ));
            }

            // ------------------------------------------------------------
            // DEPARTMENT SUBJECT
            // ------------------------------------------------------------
            // Only students belonging to the subject's department
            // can offer it.
            // ------------------------------------------------------------

            else if (subject.Type == SubjectType.Department)
            {
                query = query.Where(student =>
                    student.DepartmentId == subject.DepartmentId &&
                    _context.ClassSubjects.Any(cs =>
                        cs.ClassId == student.ClassId &&
                        cs.SubjectId == subject.Id
                    ));
            }

            // ------------------------------------------------------------
            // TRADE SUBJECT
            // ------------------------------------------------------------
            // Only students who selected this exact trade subject
            // can offer it.
            // ------------------------------------------------------------

            else if (subject.Type == SubjectType.Trade)
            {
                query = query.Where(student =>
                    student.TradeSubjectId == subject.Id &&
                    _context.ClassSubjects.Any(cs =>
                        cs.ClassId == student.ClassId &&
                        cs.SubjectId == subject.Id
                    ));
            }
        }


        // ------------------------------------------------------------
        // GET STUDENTS
        // ------------------------------------------------------------

        var students = await query
            .Select(x => new
            {
                // ----------------------------------------------------
                // STUDENT
                // ----------------------------------------------------

                StudentId = x.Id,

                StudentNumber = x.StudentNumber,

                StudentName = x.User.FullName,

                // ----------------------------------------------------
                // CLASS
                // ----------------------------------------------------

                ClassId = x.ClassId,

                ClassName = x.Class.Name,

                // ----------------------------------------------------
                // SCHOOL LEVEL
                // ----------------------------------------------------

                SchoolLevel = x.SchoolLevel.ToString(),

                SchoolLevelId = (int)x.SchoolLevel,

                // ----------------------------------------------------
                // DEPARTMENT
                // ----------------------------------------------------

                DepartmentId = x.DepartmentId,

                DepartmentName = x.Department != null
                    ? x.Department.Name
                    : null,

                // ----------------------------------------------------
                // TRADE
                // ----------------------------------------------------

                TradeId = x.TradeId,

                TradeName = x.Trade != null
                    ? x.Trade.Name
                    : null,

                // ----------------------------------------------------
                // PARENTS
                // ----------------------------------------------------

                ParentCount = x.Parents.Count()
            })
            .OrderBy(x => x.ClassName)
            .ThenBy(x => x.StudentName)
            .ToListAsync();

        // ------------------------------------------------------------
        // RESPONSE
        // ------------------------------------------------------------

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

    public async Task<(bool Success, object? Data, string? Error)>
    UpdateStudentAcademicPathAsync(
        Guid schoolId,
        Guid studentId,
        UpdateStudentAcademicPathRequest request)
    {
        // ============================================================
        // STUDENT
        // ============================================================

        var student = await _context.StudentProfiles
            .Include(s => s.Class)
            .Include(s => s.Department)
            .Include(s => s.Trade)
            .FirstOrDefaultAsync(s =>
                s.Id == studentId &&
                s.SchoolId == schoolId);

        if (student == null)
        {
            return (
                false,
                null,
                "Student not found in this school."
            );
        }


        // ============================================================
        // SENIOR / JUNIOR RULES
        // ============================================================

        if (student.Class == null)
        {
            return (
                false,
                null,
                "Student is not assigned to a class."
            );
        }


        // ============================================================
        // SENIOR STUDENT
        // ============================================================

        if (student.Class.Level == SchoolLevel.Senior)
        {
            if (!request.DepartmentId.HasValue ||
                request.DepartmentId.Value == Guid.Empty)
            {
                return (
                    false,
                    null,
                    "A department is required for senior school students."
                );
            }
        }


        // ============================================================
        // JUNIOR STUDENT
        // ============================================================

        if (student.Class.Level == SchoolLevel.Junior)
        {
            if (request.DepartmentId.HasValue &&
                request.DepartmentId.Value != Guid.Empty)
            {
                return (
                    false,
                    null,
                    "Junior school students cannot be assigned to a department."
                );
            }
        }


        // ============================================================
        // DEPARTMENT
        // ============================================================

        Department? department = null;

        if (request.DepartmentId.HasValue &&
            request.DepartmentId.Value != Guid.Empty)
        {
            department = await _context.Departments
                .FirstOrDefaultAsync(d =>
                    d.Id == request.DepartmentId.Value &&
                    d.SchoolId == schoolId);

            if (department == null)
            {
                return (
                    false,
                    null,
                    "Department not found in this school."
                );
            }
        }


        // ============================================================
        // TRADE
        // ============================================================

        Trade? trade = null;

        if (request.TradeId.HasValue &&
            request.TradeId.Value != Guid.Empty)
        {
            trade = await _context.Trades
                .FirstOrDefaultAsync(t =>
                    t.Id == request.TradeId.Value &&
                    t.SchoolId == schoolId);

            if (trade == null)
            {
                return (
                    false,
                    null,
                    "Trade not found in this school."
                );
            }
        }


        // ============================================================
        // UPDATE
        // ============================================================

        student.DepartmentId =
            department?.Id;

        student.TradeId =
            trade?.Id;

        await _context.SaveChangesAsync();


        // ============================================================
        // RESPONSE
        // ============================================================

        return (
            true,
            new
            {
                studentId = student.Id,

                classId = student.ClassId,
                className = student.Class.Name,
                level = student.Class.Level.ToString(),

                departmentId = department?.Id,
                departmentName = department?.Name,

                tradeId = trade?.Id,
                tradeName = trade?.Name,

                message =
                    "Student academic path updated successfully."
            },
            null
        );
    }

    public async Task<(bool Success, object? Data, string? Error)>
      AssignTradeAsync(
          Guid schoolId,
          Guid studentId,
          Guid tradeId,
          Guid tradeSubjectId)
    {
        // ============================================================
        // STUDENT
        // ============================================================

        var student = await _context.StudentProfiles
            .Include(x => x.User)
            .Include(x => x.Class)
            .FirstOrDefaultAsync(x =>
                x.Id == studentId &&
                x.SchoolId == schoolId);

        if (student == null)
        {
            return (
                false,
                null,
                "Student not found in this school."
            );
        }

        // ============================================================
        // TRADE
        // ============================================================

        var trade = await _context.Trades
            .FirstOrDefaultAsync(x =>
                x.Id == tradeId &&
                x.SchoolId == schoolId);

        if (trade == null)
        {
            return (
                false,
                null,
                "Trade not found in this school."
            );
        }

        // ============================================================
        // TRADE SUBJECT
        // ============================================================

        var tradeSubject = await _context.Subjects
      .FirstOrDefaultAsync(x =>
          x.Id == tradeSubjectId &&
          x.SchoolId == schoolId &&
          x.TradeId == tradeId &&
          x.Type == SubjectType.Trade);


        if (tradeSubject == null)
        {
            return (
                false,
                null,
                "The selected subject does not belong to this trade."
            );
        }

        // ============================================================
        // CHECK EXISTING SELECTION
        // ============================================================

        if (student.TradeId == tradeId &&
            student.TradeSubjectId == tradeSubjectId)
        {
            return (
                false,
                null,
                "This student has already selected this trade subject."
            );
        }

        // ============================================================
        // ASSIGN TRADE + SUBJECT
        // ============================================================

        student.TradeId = tradeId;
        student.TradeSubjectId = tradeSubjectId;

        await _context.SaveChangesAsync();

        // ============================================================
        // RESPONSE
        // ============================================================

        return (
            true,
            new
            {
                studentId = student.Id,
                studentNumber = student.StudentNumber,
                studentName = student.User.FullName,

                schoolId = student.SchoolId,

                classId = student.ClassId,
                className = student.Class.Name,

                schoolLevel = student.SchoolLevel.ToString(),
                schoolLevelId = (int)student.SchoolLevel,

                tradeId = trade.Id,
                tradeName = trade.Name,

                tradeSubjectId = tradeSubject.Id,
                tradeSubjectName = tradeSubject.Name,
                tradeSubjectCode = tradeSubject.Code
            },
            null
        );
    }



    public async Task<(bool Success, object? Data, string? Error)>
        UnassignTradeAsync(
            Guid schoolId,
            Guid studentId)
    {
        // ============================================================
        // STUDENT
        // ============================================================

        var student = await _context.StudentProfiles
            .Include(x => x.User)
            .Include(x => x.Class)
            .FirstOrDefaultAsync(x =>
                x.Id == studentId &&
                x.SchoolId == schoolId);

        if (student == null)
        {
            return (
                false,
                null,
                "Student not found in this school."
            );
        }

        // ============================================================
        // CHECK IF STUDENT HAS A TRADE
        // ============================================================

        if (!student.TradeId.HasValue)
        {
            return (
                false,
                null,
                "This student is not assigned to a trade."
            );
        }

        // ============================================================
        // REMOVE TRADE
        // ============================================================

        student.TradeId = null;
        student.TradeSubjectId = null;


        await _context.SaveChangesAsync();

        // ============================================================
        // RESPONSE
        // ============================================================

        return (
            true,
            new
            {
                studentId = student.Id,
                studentNumber = student.StudentNumber,
                studentName = student.User.FullName,

                schoolId = student.SchoolId,

                classId = student.ClassId,
                className = student.Class.Name,

                schoolLevel = student.SchoolLevel.ToString(),

                tradeId = (Guid?)null,
                tradeName = (string?)null
            },
            null
        );
    }


}