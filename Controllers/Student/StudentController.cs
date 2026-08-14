using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Controllers;

[ApiController]
[Route("api/student")]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Student}")]
public class StudentController : ControllerBase
{
    private readonly IStudentPortalService _studentService;

    public StudentController(
        IStudentPortalService studentService)
    {
        _studentService = studentService;
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(
            ClaimTypes.NameIdentifier);
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result =
            await _studentService.GetProfileAsync(userId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }

    [HttpGet("class")]
    public async Task<IActionResult> GetClass()
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result =
            await _studentService.GetClassAsync(userId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }

    [HttpGet("subjects")]
    public async Task<IActionResult> GetSubjects()
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result =
            await _studentService.GetSubjectsAsync(userId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }

    [HttpGet("results")]
    public async Task<IActionResult> GetResults(
    [FromQuery] string session,
    [FromQuery] string term)
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(session) ||
            string.IsNullOrWhiteSpace(term))
        {
            return BadRequest(new
            {
                message = "Session and term are required."
            });
        }

        var result =
            await _studentService.GetResultsAsync(
                userId,
                session,
                term);

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
    // ATTENDANCE
    // ============================================================

    [HttpGet("attendance")]
    public async Task<IActionResult> GetAttendance(
        [FromQuery] string session,
        [FromQuery] string term)
    {
        if (string.IsNullOrWhiteSpace(session))
        {
            return BadRequest(new
            {
                success = false,
                message = "Session is required."
            });
        }

        if (string.IsNullOrWhiteSpace(term))
        {
            return BadRequest(new
            {
                success = false,
                message = "Term is required."
            });
        }

        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "User identity not found."
            });
        }

        var result =
            await _studentService.GetAttendanceAsync(
                userId,
                session,
                term);

        if (!result.Success)
        {
            return NotFound(new
            {
                success = false,
                message = result.Error
            });
        }

        return Ok(new
        {
            success = true,
            data = result.Data
        });
    }

    // ============================================================
    // ASSIGNMENTS
    // ============================================================

    [HttpGet("assignments")]
    public async Task<IActionResult> GetAssignments(
        [FromQuery] string session,
        [FromQuery] string term)
    {
        if (string.IsNullOrWhiteSpace(session))
        {
            return BadRequest(new
            {
                success = false,
                message = "Session is required."
            });
        }

        if (string.IsNullOrWhiteSpace(term))
        {
            return BadRequest(new
            {
                success = false,
                message = "Term is required."
            });
        }

        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "User identity not found."
            });
        }

        var result =
            await _studentService.GetAssignmentsAsync(
                userId,
                session,
                term);

        if (!result.Success)
        {
            return NotFound(new
            {
                success = false,
                message = result.Error
            });
        }

        return Ok(new
        {
            success = true,
            data = result.Data
        });
    }

    // ============================================================
    // SINGLE ASSIGNMENT
    // ============================================================

    [HttpGet("assignments/{assignmentId:guid}")]
    public async Task<IActionResult> GetAssignment(
        Guid assignmentId)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "User identity not found."
            });
        }

        var result =
            await _studentService.GetAssignmentAsync(
                userId,
                assignmentId);

        if (!result.Success)
        {
            return NotFound(new
            {
                success = false,
                message = result.Error
            });
        }

        return Ok(new
        {
            success = true,
            data = result.Data
        });
    }

    // ============================================================
    // SUBMIT ASSIGNMENT
    // ============================================================

    [HttpPost("assignments/{assignmentId:guid}/submit")]
    public async Task<IActionResult> SubmitAssignment(
        Guid assignmentId,
        [FromBody] SubmitAssignmentRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "User identity not found."
            });
        }

        if (string.IsNullOrWhiteSpace(request.SubmissionText) &&
            string.IsNullOrWhiteSpace(request.AttachmentUrl))
        {
            return BadRequest(new
            {
                success = false,
                message =
                    "Please provide submission text or an attachment."
            });
        }

        var result =
            await _studentService.SubmitAssignmentAsync(
                userId,
                assignmentId,
                request.SubmissionText,
                request.AttachmentUrl);

        if (!result.Success)
        {
            return BadRequest(new
            {
                success = false,
                message = result.Error
            });
        }

        return Ok(new
        {
            success = true,
            message = "Assignment submitted successfully.",
            data = result.Data
        });
    }

}