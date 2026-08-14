using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Controllers;

[ApiController]
[Route("api/parent")]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Parent}")]
public class ParentController : ControllerBase
{
    private readonly IParentPortalService _parentService;

    public ParentController(
        IParentPortalService parentService)
    {
        _parentService = parentService;
    }

    // ============================================================
    // USER ID
    // ============================================================

    private string? GetUserId()
    {
        return User.FindFirstValue(
            ClaimTypes.NameIdentifier);
    }

    // ============================================================
    // PROFILE
    // ============================================================

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result =
            await _parentService.GetProfileAsync(userId);

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
    // CHILDREN
    // ============================================================

    [HttpGet("children")]
    public async Task<IActionResult> GetChildren()
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result =
            await _parentService.GetChildrenAsync(userId);

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
    // SINGLE CHILD
    // ============================================================

    [HttpGet("children/{studentId:guid}")]
    public async Task<IActionResult> GetChild(
        Guid studentId)
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result =
            await _parentService.GetChildAsync(
                userId,
                studentId);

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
    // CHILD CLASS
    // ============================================================

    [HttpGet("children/{studentId:guid}/class")]
    public async Task<IActionResult> GetChildClass(
        Guid studentId)
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result =
            await _parentService.GetChildClassAsync(
                userId,
                studentId);

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
    // CHILD SUBJECTS
    // ============================================================

    [HttpGet("children/{studentId:guid}/subjects")]
    public async Task<IActionResult> GetChildSubjects(
        Guid studentId)
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result =
            await _parentService.GetChildSubjectsAsync(
                userId,
                studentId);

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
    // CHILD RESULTS
    // ============================================================

    [HttpGet("children/{studentId:guid}/results")]
    public async Task<IActionResult> GetChildResults(
        Guid studentId,
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
            await _parentService.GetChildResultsAsync(
                userId,
                studentId,
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
    // CHILD ATTENDANCE
    // ============================================================

    [HttpGet("children/{studentId:guid}/attendance")]
    public async Task<IActionResult> GetChildAttendance(
        Guid studentId,
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
            await _parentService.GetChildAttendanceAsync(
                userId,
                studentId,
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
    // CHILD ASSIGNMENTS
    // ============================================================

    [HttpGet("children/{studentId:guid}/assignments")]
    public async Task<IActionResult> GetChildAssignments(
        Guid studentId,
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
            await _parentService.GetChildAssignmentsAsync(
                userId,
                studentId,
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
    // SINGLE CHILD ASSIGNMENT
    // ============================================================

    [HttpGet(
        "children/{studentId:guid}/assignments/{assignmentId:guid}")]
    public async Task<IActionResult> GetChildAssignment(
        Guid studentId,
        Guid assignmentId)
    {
        var userId = GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result =
            await _parentService.GetChildAssignmentAsync(
                userId,
                studentId,
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
}