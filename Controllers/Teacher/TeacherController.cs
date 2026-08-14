using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.Results;
using UserManagementApi.Services;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Controllers;

[ApiController]
[Route("api/teacher")]
[Authorize(Roles = Roles.Teacher)]
public class TeacherController(
    ITeacherPortalService teacherService,
    ICurrentUserService currentUser,
    ApplicationDbContext context) : ControllerBase
{
    private readonly ITeacherPortalService _teacherService = teacherService;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ApplicationDbContext _context = context;

    protected Guid? GetSchoolId()
    {
        var value = User.FindFirst("SchoolId")?.Value;

        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Guid.TryParse(value, out var schoolId)
            ? schoolId
            : null;
    }

    // ============================================================
    // PROFILE
    // ============================================================

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = _currentUser.GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result =
            await _teacherService.GetProfileAsync(userId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }

    // ============================================================
    // CLASSES
    // ============================================================

    [HttpGet("classes")]
    public async Task<IActionResult> GetClasses()
    {
        var userId = _currentUser.GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result =
            await _teacherService.GetClassesAsync(userId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }

    // ============================================================
    // SUBJECTS
    // ============================================================

    [HttpGet("subjects")]
    public async Task<IActionResult> GetSubjects()
    {
        var userId = _currentUser.GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result =
            await _teacherService.GetSubjectsAsync(userId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }

    // ============================================================
    // RESULTS
    // ============================================================

    [HttpGet("results")]
    public async Task<IActionResult> GetResults(
     [FromQuery] GetTeacherResultsRequest request)
    {
        // ============================================================
        // GET LOGGED-IN USER
        // ============================================================

        var userId = _currentUser.GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        // ============================================================
        // FIND TEACHER PROFILE
        // ============================================================

        var teacher = await _context.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.UserId == userId);

        if (teacher == null)
        {
            return NotFound(new
            {
                message = "Teacher profile not found."
            });
        }

        // ============================================================
        // GET SCHOOL FROM TEACHER
        // ============================================================

        var schoolId = teacher.SchoolId;

        // ============================================================
        // GET RESULTS
        // ============================================================

        var result = await _teacherService.GetResultsAsync(
            schoolId,
            teacher.Id,
            request);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }

    // ============================================================
    // CREATE RESULT
    // ============================================================

    [HttpPost("results")]
    public async Task<IActionResult> CreateResult(
        [FromBody] CreateResultRequest request)
    {
        var userId = _currentUser.GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.CreateResultAsync(
            userId,
            request);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ============================================================
    // UPDATE RESULT
    // ============================================================

    [HttpPut("results/{resultId:guid}")]
    public async Task<IActionResult> UpdateResult(
        Guid resultId,
        [FromBody] CreateResultRequest request)
    {
        var userId = _currentUser.GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.UpdateResultAsync(
            userId,
            resultId,
            request);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ============================================================
    // DELETE RESULT
    // ============================================================

    [HttpDelete("results/{resultId:guid}")]
    public async Task<IActionResult> DeleteResult(
        Guid resultId)
    {
        var userId = _currentUser.GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.DeleteResultAsync(
            userId,
            resultId);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Result deleted successfully."
        });
    }
}