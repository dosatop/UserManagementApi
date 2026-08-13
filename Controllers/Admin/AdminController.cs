using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.Models;

namespace UserManagementApi.Controllers;

public abstract class SchoolAdminControllerBase : ControllerBase
{
    protected Guid? GetSchoolId()
    {
        var value = User.FindFirst("SchoolId")?.Value;

        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Guid.TryParse(value, out var schoolId)
            ? schoolId
            : null;
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Admin)]
public class AdminController(
    ApplicationDbContext context,
    UserManager<User> userManager) : SchoolAdminControllerBase
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<User> _userManager = userManager;

    // ================================================================
    // GET SCHOOL ID FROM LOGGED-IN ADMIN
    // ================================================================

    private Guid? GetSchoolId()
    {
        var schoolIdClaim = User.FindFirst("SchoolId")?.Value;

        if (string.IsNullOrWhiteSpace(schoolIdClaim))
        {
            return null;
        }

        if (!Guid.TryParse(schoolIdClaim, out var schoolId))
        {
            return null;
        }

        return schoolId;
    }

    // ================================================================
    // DASHBOARD
    // ================================================================

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        // Check school
        var school = await _context.Schools
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == schoolId);

        if (school == null)
        {
            return NotFound(new
            {
                message = "School not found."
            });
        }

        // ============================================================
        // COUNTS
        // ============================================================

        var userCount = await _context.Users
            .CountAsync(x => x.SchoolId == schoolId);

        var adminCount = (
            from user in _context.Users
            join userRole in _context.UserRoles
                on user.Id equals userRole.UserId
            join role in _context.Roles
                on userRole.RoleId equals role.Id
            where user.SchoolId == schoolId
                  && role.Name == Roles.Admin
            select user.Id
        ).Count();

        var teacherCount = await _context.Teachers
            .CountAsync(x => x.SchoolId == schoolId);

        var studentCount = await _context.StudentProfiles
            .CountAsync(x => x.SchoolId == schoolId);

        var classCount = await _context.Classes
            .CountAsync(x => x.SchoolId == schoolId);

        var subjectCount = await _context.Subjects
            .CountAsync(x => x.SchoolId == schoolId);

        return Ok(new
        {
            schoolId = school.Id,
            schoolName = school.Name,

            userCount,
            adminCount,
            teacherCount,
            studentCount,
            classCount,
            subjectCount
        });
    }

    // ================================================================
    // GET TEACHERS
    // ================================================================

    [HttpGet("teachers")]
    public async Task<IActionResult> GetTeachers()
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var teachers = await _context.Teachers
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .ToListAsync();

        return Ok(teachers);
    }

    // ================================================================
    // GET STUDENTS
    // ================================================================

    [HttpGet("students")]
    public async Task<IActionResult> GetStudents()
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var students = await _context.StudentProfiles
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .ToListAsync();

        return Ok(students);
    }

    // ================================================================
    // GET CLASSES
    // ================================================================

    [HttpGet("classes")]
    public async Task<IActionResult> GetClasses()
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var classes = await _context.Classes
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .ToListAsync();

        return Ok(classes);
    }

    // ================================================================
    // GET SUBJECTS
    // ================================================================

    [HttpGet("subjects")]
    public async Task<IActionResult> GetSubjects()
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var subjects = await _context.Subjects
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .ToListAsync();

        return Ok(subjects);
    }
}