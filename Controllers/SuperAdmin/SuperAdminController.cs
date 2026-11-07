using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;               // ← ApplicationDbContext namespace
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.Users;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Controllers;

[ApiController]
[Route("api/super-admin")]
[Authorize(Roles = Roles.SuperAdmin)]
public class SuperAdminController(
    IUserManagementService userManagementService,
    ApplicationDbContext context)
    : ControllerBase
{
    private readonly IUserManagementService
        _userManagementService = userManagementService;

    private readonly ApplicationDbContext _context = context;

    // ================================================================
    // CREATE SCHOOL ADMIN
    // ================================================================

    [HttpPost("schools/{schoolId:guid}/admin")]
    public async Task<IActionResult> CreateSchoolAdmin(
        Guid schoolId,
        CreateSchoolAdminRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result =
            await _userManagementService.CreateSchoolAdminAsync(
                schoolId,
                request.FullName,
                request.Email,
                request.PhoneNumber,
                request.Password);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
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
    // SCHOOL TEACHERS
    // GET api/super-admin/schools/{schoolId}/teachers
    // ================================================================

    [HttpGet("schools/{schoolId:guid}/teachers")]
    public async Task<IActionResult> GetSchoolTeachers(Guid schoolId)
    {
        if (!await SchoolExists(schoolId))
            return NotFound(new { message = "School not found." });

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

        return Ok(new { teacherCount = result.Count, teachers = result });
    }

    // ================================================================
    // SCHOOL STUDENTS
    // GET api/super-admin/schools/{schoolId}/students?classId=
    // ================================================================

    [HttpGet("schools/{schoolId:guid}/students")]
    public async Task<IActionResult> GetSchoolStudents(
        Guid schoolId,
        [FromQuery] Guid? classId = null)
    {
        if (!await SchoolExists(schoolId))
            return NotFound(new { message = "School not found." });

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

        return Ok(new { studentCount = students.Count, classId, students });
    }

    // ================================================================
    // SCHOOL CLASSES
    // GET api/super-admin/schools/{schoolId}/classes
    // ================================================================

    [HttpGet("schools/{schoolId:guid}/classes")]
    public async Task<IActionResult> GetSchoolClasses(Guid schoolId)
    {
        if (!await SchoolExists(schoolId))
            return NotFound(new { message = "School not found." });

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

        return Ok(new { classCount = result.Count, classes = result });
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
}