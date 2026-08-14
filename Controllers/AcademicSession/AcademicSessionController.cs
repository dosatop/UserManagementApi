using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementApi.DTOs.AcademicSessions;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Controllers;

[ApiController]
[Route("api/academic-sessions")]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Student}")]
public class AcademicSessionController(
    IAcademicSessionService academicSessionService) : ControllerBase
{
    private readonly IAcademicSessionService _academicSessionService =
        academicSessionService;

    // ================================================================
    // SCHOOL ID
    // ================================================================

    private Guid? GetSchoolId()
    {
        var value = User.FindFirst("SchoolId")?.Value;

        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Guid.TryParse(value, out var schoolId)
            ? schoolId
            : null;
    }

    // ================================================================
    // GET ALL
    // ================================================================

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var schoolId = GetSchoolId();

        if (!schoolId.HasValue)
        {
            return Unauthorized(new
            {
                message = "School could not be determined."
            });
        }

        var result =
            await _academicSessionService.GetAllAsync(
                schoolId.Value);

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
    // GET CURRENT
    // ================================================================

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent()
    {
        var schoolId = GetSchoolId();

        if (!schoolId.HasValue)
        {
            return Unauthorized(new
            {
                message = "School could not be determined."
            });
        }

        var result =
            await _academicSessionService.GetCurrentAsync(
                schoolId.Value);

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
    // GET BY ID
    // ================================================================

    [HttpGet("{sessionId:guid}")]
    public async Task<IActionResult> GetById(
        Guid sessionId)
    {
        var schoolId = GetSchoolId();

        if (!schoolId.HasValue)
        {
            return Unauthorized(new
            {
                message = "School could not be determined."
            });
        }

        var result =
            await _academicSessionService.GetByIdAsync(
                schoolId.Value,
                sessionId);

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
    // CREATE
    // ================================================================

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAcademicSessionRequest request)
    {
        var schoolId = GetSchoolId();

        if (!schoolId.HasValue)
        {
            return Unauthorized(new
            {
                message = "School could not be determined."
            });
        }

        var result =
            await _academicSessionService.CreateAsync(
                schoolId.Value,
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
    // UPDATE
    // ================================================================

    [HttpPut("{sessionId:guid}")]
    public async Task<IActionResult> Update(
        Guid sessionId,
        [FromBody] UpdateAcademicSessionRequest request)
    {
        var schoolId = GetSchoolId();

        if (!schoolId.HasValue)
        {
            return Unauthorized(new
            {
                message = "School could not be determined."
            });
        }

        var result =
            await _academicSessionService.UpdateAsync(
                schoolId.Value,
                sessionId,
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
    // ACTIVATE
    // ================================================================

    [HttpPut("{sessionId:guid}/activate")]
    public async Task<IActionResult> Activate(
        Guid sessionId)
    {
        var schoolId = GetSchoolId();

        if (!schoolId.HasValue)
        {
            return Unauthorized(new
            {
                message = "School could not be determined."
            });
        }

        var result =
            await _academicSessionService.ActivateAsync(
                schoolId.Value,
                sessionId);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Academic session activated successfully.",
            data = result.Data
        });
    }

    // ================================================================
    // DELETE
    // ================================================================

    [HttpDelete("{sessionId:guid}")]
    public async Task<IActionResult> Delete(
        Guid sessionId)
    {
        var schoolId = GetSchoolId();

        if (!schoolId.HasValue)
        {
            return Unauthorized(new
            {
                message = "School could not be determined."
            });
        }

        var result =
            await _academicSessionService.DeleteAsync(
                schoolId.Value,
                sessionId);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Academic session deleted successfully."
        });
    }
}