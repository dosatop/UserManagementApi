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

     
// ====================================================================
// 3. StudentController — add this helper inside the class
// ====================================================================
// Uses the session/term from the query if given, otherwise the
// school's current ones. Returns an error message if neither exists.
 
private async Task<(string? Session, string? Term, string? Error)>
    ResolvePeriodAsync(string userId, string? session, string? term)
{
    if (!string.IsNullOrWhiteSpace(session) &&
        !string.IsNullOrWhiteSpace(term))
    {
        return (session.Trim(), term.Trim(), null);
    }
 
    var current = await _studentService.GetCurrentPeriodAsync(userId);
 
    var resolvedSession = string.IsNullOrWhiteSpace(session)
        ? current.Session
        : session.Trim();
 
    var resolvedTerm = string.IsNullOrWhiteSpace(term)
        ? current.Term
        : term.Trim();
 
    if (string.IsNullOrWhiteSpace(resolvedSession))
    {
        return (null, null, "There is no active academic session.");
    }
 
    if (string.IsNullOrWhiteSpace(resolvedTerm))
    {
        return (null, null, "There is no active academic term.");
    }
 
    return (resolvedSession, resolvedTerm, null);
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

   // ====================================================================
// 4. StudentController — replace the three actions below
// ====================================================================
 
[HttpGet("results")]
public async Task<IActionResult> GetResults(
    [FromQuery] string? session,
    [FromQuery] string? term)
{
    var userId = GetUserId();
 
    if (string.IsNullOrEmpty(userId))
    {
        return Unauthorized();
    }
 
    var period = await ResolvePeriodAsync(userId, session, term);
 
    if (period.Error != null)
    {
        return BadRequest(new { message = period.Error });
    }
 
    var result = await _studentService.GetResultsAsync(
        userId,
        period.Session!,
        period.Term!);
 
    if (!result.Success)
    {
        return NotFound(new { message = result.Error });
    }
 
    return Ok(result.Data);
}
 
// ============================================================
// ATTENDANCE
// ============================================================
 
[HttpGet("attendance")]
public async Task<IActionResult> GetAttendance(
    [FromQuery] string? session,
    [FromQuery] string? term)
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
 
    var period = await ResolvePeriodAsync(userId, session, term);
 
    if (period.Error != null)
    {
        return BadRequest(new { success = false, message = period.Error });
    }
 
    var result = await _studentService.GetAttendanceAsync(
        userId,
        period.Session!,
        period.Term!);
 
    if (!result.Success)
    {
        return NotFound(new { success = false, message = result.Error });
    }
 
    return Ok(new { success = true, data = result.Data });
}
 
// ============================================================
// ASSIGNMENTS
// ============================================================
 
[HttpGet("assignments")]
public async Task<IActionResult> GetAssignments(
    [FromQuery] string? session,
    [FromQuery] string? term)
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
 
    var period = await ResolvePeriodAsync(userId, session, term);
 
    if (period.Error != null)
    {
        return BadRequest(new { success = false, message = period.Error });
    }
 
    var result = await _studentService.GetAssignmentsAsync(
        userId,
        period.Session!,
        period.Term!);
 
    if (!result.Success)
    {
        return NotFound(new { success = false, message = result.Error });
    }
 
    return Ok(new { success = true, data = result.Data });
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