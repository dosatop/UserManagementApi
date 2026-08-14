using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.Results;
using UserManagementApi.DTOs.TeacherPortal;
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


    // ================================================================
    // CURRENT USER
    // ================================================================

    private string? GetUserId()
    {
        return _currentUser.GetUserId()
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
    }


    // ================================================================
    // PROFILE
    // ================================================================

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.GetProfileAsync(userId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ================================================================
    // CLASSES
    // ================================================================

    [HttpGet("classes")]
    public async Task<IActionResult> GetClasses()
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.GetClassesAsync(userId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ================================================================
    // SUBJECTS
    // ================================================================

    [HttpGet("subjects")]
    public async Task<IActionResult> GetSubjects()
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.GetSubjectsAsync(userId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ================================================================
    // RESULTS
    // ================================================================

    [HttpGet("results")]
    public async Task<IActionResult> GetResults(
        [FromQuery] GetTeacherResultsRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

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

        var result = await _teacherService.GetResultsAsync(
            teacher.SchoolId,
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


    // ================================================================
    // CREATE RESULT
    // ================================================================

    [HttpPost("results")]
    public async Task<IActionResult> CreateResult(
        [FromBody] CreateResultRequest request)
    {
        var userId = GetUserId();

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


    // ================================================================
    // UPDATE RESULT
    // ================================================================

    [HttpPut("results/{resultId:guid}")]
    public async Task<IActionResult> UpdateResult(
        Guid resultId,
        [FromBody] CreateResultRequest request)
    {
        var userId = GetUserId();

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


    // ================================================================
    // DELETE RESULT
    // ================================================================

    [HttpDelete("results/{resultId:guid}")]
    public async Task<IActionResult> DeleteResult(
        Guid resultId)
    {
        var userId = GetUserId();

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


    // ================================================================
    // ASSIGNMENTS
    // ================================================================

    [HttpGet("assignments")]
    public async Task<IActionResult> GetAssignments(
        [FromQuery] GetTeacherAssignmentsRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.GetAssignmentsAsync(
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


    // ================================================================
    // SINGLE ASSIGNMENT
    // ================================================================

    [HttpGet("assignments/{assignmentId:guid}")]
    public async Task<IActionResult> GetAssignment(
        Guid assignmentId)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.GetAssignmentAsync(
            userId,
            assignmentId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ================================================================
    // CREATE ASSIGNMENT
    // ================================================================

    [HttpPost("assignments")]
    public async Task<IActionResult> CreateAssignment(
        [FromBody] CreateAssignmentRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.CreateAssignmentAsync(
            userId,
            request);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        // The service returns an anonymous object containing
        // assignmentId, so return the created data directly.
        return StatusCode(
            StatusCodes.Status201Created,
            result.Data);
    }


    // ================================================================
    // UPDATE ASSIGNMENT
    // ================================================================

    [HttpPut("assignments/{assignmentId:guid}")]
    public async Task<IActionResult> UpdateAssignment(
        Guid assignmentId,
        [FromBody] UpdateAssignmentRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.UpdateAssignmentAsync(
            userId,
            assignmentId,
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


    // ================================================================
    // DELETE ASSIGNMENT
    // ================================================================

    [HttpDelete("assignments/{assignmentId:guid}")]
    public async Task<IActionResult> DeleteAssignment(
        Guid assignmentId)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.DeleteAssignmentAsync(
            userId,
            assignmentId);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Assignment deleted successfully."
        });
    }


    // ================================================================
    // ATTENDANCE
    // ================================================================

    [HttpGet("attendance")]
    public async Task<IActionResult> GetAttendance(
        [FromQuery] GetTeacherAttendanceRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.GetAttendanceAsync(
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


    // ================================================================
    // SINGLE ATTENDANCE RECORD
    // ================================================================

    [HttpGet("attendance/{attendanceId:guid}")]
    public async Task<IActionResult> GetAttendanceRecord(
        Guid attendanceId)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.GetAttendanceRecordAsync(
            userId,
            attendanceId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ================================================================
    // CREATE ATTENDANCE
    // ================================================================

    [HttpPost("attendance")]
    public async Task<IActionResult> CreateAttendance(
        [FromBody] CreateAttendanceRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.CreateAttendanceAsync(
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


    // ================================================================
    // UPDATE ATTENDANCE
    // ================================================================

    [HttpPut("attendance/{attendanceId:guid}")]
    public async Task<IActionResult> UpdateAttendance(
        Guid attendanceId,
        [FromBody] UpdateAttendanceRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.UpdateAttendanceAsync(
            userId,
            attendanceId,
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


    // ================================================================
    // DELETE ATTENDANCE
    // ================================================================

    [HttpDelete("attendance/{attendanceId:guid}")]
    public async Task<IActionResult> DeleteAttendance(
        Guid attendanceId)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "User identity could not be determined."
            });
        }

        var result = await _teacherService.DeleteAttendanceAsync(
            userId,
            attendanceId);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Attendance record deleted successfully."
        });
    }
}