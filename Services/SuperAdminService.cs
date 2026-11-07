using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;               // ← ApplicationDbContext namespace
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.SuperAdmin;
using UserManagementApi.DTOs.Users;
using UserManagementApi.Models;             // ← User, Teacher, StudentProfile, Class
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

// ====================================================================
// SUPER ADMIN SERVICE
// ====================================================================
// Lets the super admin view and create a school's teachers, students
// and classes, and create school admins.
//
// Register in Program.cs:
//   builder.Services.AddScoped<ISuperAdminService, SuperAdminService>();

public class SuperAdminService(
    ApplicationDbContext context,
    UserManager<User> userManager,
    IUserManagementService userManagementService)
    : ISuperAdminService
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<User> _userManager = userManager;
    private readonly IUserManagementService _userManagementService =
        userManagementService;

    // ================================================================
    // CREATE SCHOOL ADMIN
    // ================================================================

    public async Task<SuperAdminResult> CreateSchoolAdminAsync(
        Guid schoolId,
        CreateSchoolAdminRequest request)
    {
        if (!await SchoolExists(schoolId))
            return SuperAdminResult.Missing("School not found.");

        var result = await _userManagementService.CreateSchoolAdminAsync(
            schoolId,
            request.FullName,
            request.Email,
            request.PhoneNumber,
            request.Password);

        if (!result.Success)
            return SuperAdminResult.Fail(result.Error ?? "Could not create the admin.");

        return SuperAdminResult.Ok(new
        {
            message = "School admin created successfully.",
            admin = new
            {
                id = result.User!.Id,
                fullName = result.User.FullName,
                email = result.User.Email,
                phoneNumber = result.User.PhoneNumber,
                schoolId = result.User.SchoolId,
                role = Roles.Admin
            }
        });
    }

    // ================================================================
    // TEACHERS — LIST
    // ================================================================

    public async Task<SuperAdminResult> GetSchoolTeachersAsync(Guid schoolId)
    {
        if (!await SchoolExists(schoolId))
            return SuperAdminResult.Missing("School not found.");

        var teachers = await _context.Teachers
            .AsNoTracking()
            .Where(t => t.SchoolId == schoolId)
            .Select(t => new
            {
                teacherId = t.Id,
                fullName = t.User.FullName,
                email = t.User.Email,
                phoneNumber = t.User.PhoneNumber
            })
            .OrderBy(t => t.fullName)
            .ToListAsync();

        var classNames = await ClassNames(schoolId);
        var classTeachers = await ClassTeacherPairs(schoolId);

        var subjectRows = await _context.TeacherSubjects
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .Select(x => new
            {
                x.TeacherId,
                x.SubjectId,
                subjectName = x.Subject.Name,
                x.ClassId
            })
            .ToListAsync();

        var result = teachers.Select(t =>
        {
            var classTeacherOf = classTeachers
                .Where(p => p.TeacherId == t.teacherId)
                .Select(p => p.ClassId)
                .Distinct()
                .Select(id => new
                {
                    classId = id,
                    className = classNames.GetValueOrDefault(id, "Class")
                })
                .OrderBy(c => c.className)
                .ToList();

            var subjects = subjectRows
                .Where(s => s.TeacherId == t.teacherId)
                .Select(s => new
                {
                    subjectId = s.SubjectId,
                    s.subjectName,
                    classId = s.ClassId,
                    className = classNames.GetValueOrDefault(s.ClassId, "Class")
                })
                .Distinct()
                .OrderBy(s => s.subjectName)
                .ThenBy(s => s.className)
                .ToList();

            var classCount = subjects.Select(s => s.classId)
                .Concat(classTeacherOf.Select(c => c.classId))
                .Distinct()
                .Count();

            return new
            {
                t.teacherId,
                t.fullName,
                t.email,
                t.phoneNumber,
                isClassTeacher = classTeacherOf.Count > 0,
                classTeacherOf,
                subjects,
                subjectCount = subjects.Select(s => s.subjectId).Distinct().Count(),
                classCount
            };
        }).ToList();

        return SuperAdminResult.Ok(new
        {
            teacherCount = result.Count,
            teachers = result
        });
    }

    // ================================================================
    // TEACHERS — CREATE
    // ================================================================

    public async Task<SuperAdminResult> CreateSchoolTeacherAsync(
        Guid schoolId,
        SuperAdminCreateTeacherRequest request)
    {
        if (!await SchoolExists(schoolId))
            return SuperAdminResult.Missing("School not found.");

        var email = request.Email.Trim().ToLowerInvariant();

        if (await _userManager.FindByEmailAsync(email) != null)
            return SuperAdminResult.Fail("A user with this email already exists.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var (user, error) = await CreateUserAsync(
                schoolId,
                request.FullName,
                email,
                request.PhoneNumber,
                request.Password,
                Roles.Teacher);

            if (user == null)
            {
                await transaction.RollbackAsync();
                return SuperAdminResult.Fail(error!);
            }

            var teacher = new Teacher
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                SchoolId = schoolId
            };

            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return SuperAdminResult.Ok(new
            {
                message = "Teacher created successfully.",
                teacher = new
                {
                    teacherId = teacher.Id,
                    fullName = user.FullName,
                    email = user.Email,
                    phoneNumber = user.PhoneNumber,
                    isClassTeacher = false,
                    classTeacherOf = Array.Empty<object>(),
                    subjects = Array.Empty<object>(),
                    subjectCount = 0,
                    classCount = 0
                }
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return SuperAdminResult.Fail($"Could not create the teacher: {ex.Message}");
        }
    }

    // ================================================================
    // STUDENTS — LIST
    // ================================================================

    public async Task<SuperAdminResult> GetSchoolStudentsAsync(
        Guid schoolId,
        Guid? classId = null)
    {
        if (!await SchoolExists(schoolId))
            return SuperAdminResult.Missing("School not found.");

        var students = await _context.StudentProfiles
            .AsNoTracking()
            .Where(s =>
                s.SchoolId == schoolId &&
                (!classId.HasValue || s.ClassId == classId.Value))
            .Select(s => new
            {
                studentId = s.Id,
                fullName = s.User.FullName,
                studentNumber = s.StudentNumber,
                email = s.User.Email,
                phoneNumber = s.User.PhoneNumber,

                classId = s.ClassId,
                className = s.Class.Name,

                departmentId = s.DepartmentId,
                departmentName = _context.Departments
                    .Where(d => d.Id == s.DepartmentId)
                    .Select(d => d.Name)
                    .FirstOrDefault(),

                tradeId = s.TradeId,
                tradeName = _context.Trades
                    .Where(tr => tr.Id == s.TradeId)
                    .Select(tr => tr.Name)
                    .FirstOrDefault()
            })
            .OrderBy(s => s.className)
            .ThenBy(s => s.fullName)
            .ToListAsync();

        return SuperAdminResult.Ok(new
        {
            studentCount = students.Count,
            classId,
            students
        });
    }

    // ================================================================
    // STUDENTS — CREATE
    // ================================================================

    public async Task<SuperAdminResult> CreateSchoolStudentAsync(
        Guid schoolId,
        SuperAdminCreateStudentRequest request)
    {
        if (!await SchoolExists(schoolId))
            return SuperAdminResult.Missing("School not found.");

        var email = request.Email.Trim().ToLowerInvariant();

        if (await _userManager.FindByEmailAsync(email) != null)
            return SuperAdminResult.Fail("A user with this email already exists.");

        // ---- class --------------------------------------------------
        var schoolClass = await _context.Classes
            .AsNoTracking()
            .Where(c => c.Id == request.ClassId && c.SchoolId == schoolId)
            .Select(c => new { c.Id, c.Name })
            .FirstOrDefaultAsync();

        if (schoolClass == null)
            return SuperAdminResult.Fail("The selected class does not belong to this school.");

        // ---- department (optional) -----------------------------------
        string? departmentName = null;

        if (request.DepartmentId.HasValue)
        {
            departmentName = await _context.Departments
                .AsNoTracking()
                .Where(d => d.Id == request.DepartmentId.Value && d.SchoolId == schoolId)
                .Select(d => d.Name)
                .FirstOrDefaultAsync();

            if (departmentName == null)
                return SuperAdminResult.Fail("The selected department does not belong to this school.");
        }

        // ---- trade + trade subject (both optional) -------------------
        var tradeId = request.TradeId;

        if (request.TradeSubjectId.HasValue)
        {
            var subjectTradeId = await _context.Subjects
                .AsNoTracking()
                .Where(s => s.Id == request.TradeSubjectId.Value && s.SchoolId == schoolId)
                .Select(s => s.TradeId)
                .FirstOrDefaultAsync();

            if (subjectTradeId == null)
                return SuperAdminResult.Fail("The selected trade subject is not a trade subject of this school.");

            if (tradeId.HasValue && tradeId != subjectTradeId)
                return SuperAdminResult.Fail("The trade subject does not belong to the selected trade.");

            // Trade subject chosen without a trade → use its trade.
            tradeId = subjectTradeId;
        }

        string? tradeName = null;

        if (tradeId.HasValue)
        {
            tradeName = await _context.Trades
                .AsNoTracking()
                .Where(t => t.Id == tradeId.Value && t.SchoolId == schoolId)
                .Select(t => t.Name)
                .FirstOrDefaultAsync();

            if (tradeName == null)
                return SuperAdminResult.Fail("The selected trade does not belong to this school.");
        }

        // ---- student number -----------------------------------------
        var studentNumber = request.StudentNumber?.Trim();

        if (string.IsNullOrEmpty(studentNumber))
        {
            studentNumber = await GenerateStudentNumberAsync(schoolId);
        }
        else if (await _context.StudentProfiles.AnyAsync(s =>
                     s.SchoolId == schoolId && s.StudentNumber == studentNumber))
        {
            return SuperAdminResult.Fail("This student number is already in use.");
        }

        // ---- create -------------------------------------------------
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var (user, error) = await CreateUserAsync(
                schoolId,
                request.FullName,
                email,
                request.PhoneNumber,
                request.Password,
                Roles.Student);

            if (user == null)
            {
                await transaction.RollbackAsync();
                return SuperAdminResult.Fail(error!);
            }

            var student = new StudentProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                SchoolId = schoolId,
                ClassId = schoolClass.Id,
                StudentNumber = studentNumber,
                DepartmentId = request.DepartmentId,
                TradeId = tradeId,
                TradeSubjectId = request.TradeSubjectId
            };

            _context.StudentProfiles.Add(student);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return SuperAdminResult.Ok(new
            {
                message = "Student created successfully.",
                student = new
                {
                    studentId = student.Id,
                    fullName = user.FullName,
                    studentNumber,
                    email = user.Email,
                    phoneNumber = user.PhoneNumber,
                    classId = schoolClass.Id,
                    className = schoolClass.Name,
                    departmentId = request.DepartmentId,
                    departmentName,
                    tradeId,
                    tradeName
                }
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return SuperAdminResult.Fail($"Could not create the student: {ex.Message}");
        }
    }

    // ================================================================
    // CLASSES — LIST
    // ================================================================

    public async Task<SuperAdminResult> GetSchoolClassesAsync(Guid schoolId)
    {
        if (!await SchoolExists(schoolId))
            return SuperAdminResult.Missing("School not found.");

        var classes = await _context.Classes
            .AsNoTracking()
            .Where(c => c.SchoolId == schoolId)
            .Select(c => new
            {
                classId = c.Id,
                className = c.Name,
                studentCount = _context.StudentProfiles
                    .Count(s => s.ClassId == c.Id),
                subjectCount = _context.ClassSubjects
                    .Count(cs => cs.ClassId == c.Id)
            })
            .OrderBy(c => c.className)
            .ToListAsync();

        var teacherNames = await _context.Teachers
            .AsNoTracking()
            .Where(t => t.SchoolId == schoolId)
            .Select(t => new { t.Id, name = t.User.FullName })
            .ToDictionaryAsync(t => t.Id, t => t.name);

        var classTeachers = await ClassTeacherPairs(schoolId);

        var subjectTeachers = await _context.TeacherSubjects
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .Select(x => new
            {
                x.ClassId,
                x.TeacherId,
                x.SubjectId,
                subjectName = x.Subject.Name
            })
            .ToListAsync();

        var result = classes.Select(c => new
        {
            c.classId,
            c.className,
            c.studentCount,
            c.subjectCount,

            classTeachers = classTeachers
                .Where(p => p.ClassId == c.classId)
                .Select(p => p.TeacherId)
                .Distinct()
                .Select(id => new
                {
                    teacherId = id,
                    teacherName = teacherNames.GetValueOrDefault(id, "Teacher")
                })
                .ToList(),

            subjectTeachers = subjectTeachers
                .Where(s => s.ClassId == c.classId)
                .Select(s => new
                {
                    subjectId = s.SubjectId,
                    s.subjectName,
                    teacherId = s.TeacherId,
                    teacherName = teacherNames.GetValueOrDefault(s.TeacherId, "Teacher")
                })
                .Distinct()
                .OrderBy(s => s.subjectName)
                .ToList()
        }).ToList();

        return SuperAdminResult.Ok(new
        {
            classCount = result.Count,
            classes = result
        });
    }

    // ================================================================
    // CLASSES — CREATE
    // ================================================================

    public async Task<SuperAdminResult> CreateSchoolClassAsync(
        Guid schoolId,
        SuperAdminCreateClassRequest request)
    {
        if (!await SchoolExists(schoolId))
            return SuperAdminResult.Missing("School not found.");

        var name = request.Name.Trim();

        if (string.IsNullOrEmpty(name))
            return SuperAdminResult.Fail("Class name is required.");

        var exists = await _context.Classes.AnyAsync(c =>
            c.SchoolId == schoolId &&
            c.Name.ToLower() == name.ToLower());

        if (exists)
            return SuperAdminResult.Fail($"A class named \"{name}\" already exists in this school.");

        var schoolClass = new Class
        {
            Id = Guid.NewGuid(),
            Name = name,
            SchoolId = schoolId,

            // ← Uncomment if your Class entity has a Level (enum) property,
            //   using your enum's name:
            // Level = (SchoolLevel)(request.Level ?? 0),
        };

        _context.Classes.Add(schoolClass);
        await _context.SaveChangesAsync();

        return SuperAdminResult.Ok(new
        {
            message = "Class created successfully.",
            @class = new
            {
                classId = schoolClass.Id,
                className = schoolClass.Name,
                studentCount = 0,
                subjectCount = 0,
                classTeachers = Array.Empty<object>(),
                subjectTeachers = Array.Empty<object>()
            }
        });
    }

    // ================================================================
    // HELPERS
    // ================================================================

    private Task<bool> SchoolExists(Guid schoolId) =>
        _context.Schools.AnyAsync(s => s.Id == schoolId);

    private async Task<Dictionary<Guid, string>> ClassNames(Guid schoolId) =>
        await _context.Classes
            .AsNoTracking()
            .Where(c => c.SchoolId == schoolId)
            .ToDictionaryAsync(c => c.Id, c => c.Name);

    // Class teachers can be in TeacherClasses or ClassTeachers,
    // so read both (same as the teacher service).
    private async Task<List<(Guid TeacherId, Guid ClassId)>> ClassTeacherPairs(
        Guid schoolId)
    {
        var a = await _context.TeacherClasses
            .AsNoTracking()
            .Where(x => x.Class.SchoolId == schoolId)
            .Select(x => new { x.TeacherId, x.ClassId })
            .ToListAsync();

        var b = await _context.ClassTeachers
            .AsNoTracking()
            .Where(x => x.Class.SchoolId == schoolId)
            .Select(x => new { x.TeacherId, x.ClassId })
            .ToListAsync();

        return a.Concat(b)
            .Select(x => (x.TeacherId, x.ClassId))
            .Distinct()
            .ToList();
    }

    // Creates the login account. UserName = email so the user signs in
    // with the email + password exactly as entered (no trimming of the
    // password).
    private async Task<(User? User, string? Error)> CreateUserAsync(
        Guid schoolId,
        string fullName,
        string email,
        string? phoneNumber,
        string password,
        string role)
    {
        var user = new User
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber)
                ? null
                : phoneNumber.Trim(),
            SchoolId = schoolId
        };

        var created = await _userManager.CreateAsync(user, password);

        if (!created.Succeeded)
            return (null, IdentityError(created));

        var roleResult = await _userManager.AddToRoleAsync(user, role);

        if (!roleResult.Succeeded)
            return (null, IdentityError(roleResult));

        return (user, null);
    }

    private static string IdentityError(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(e => e.Description));

    // e.g. STU-2026-48213, unique within the school.
    private async Task<string> GenerateStudentNumberAsync(Guid schoolId)
    {
        var year = DateTime.UtcNow.Year;

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var number = $"STU-{year}-{Random.Shared.Next(10000, 99999)}";

            var taken = await _context.StudentProfiles.AnyAsync(s =>
                s.SchoolId == schoolId && s.StudentNumber == number);

            if (!taken) return number;
        }

        return $"STU-{year}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }
}
