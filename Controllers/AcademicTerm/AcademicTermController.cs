using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserManagementApi.DTOs.AcademicTerms;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Controllers;

[ApiController]
[Route("api/academic-terms")]
[Authorize]
public class AcademicTermsController(
IAcademicTermService academicTermService) : ControllerBase
{
private readonly IAcademicTermService _academicTermService =
academicTermService;

// ================================================================
// GET SCHOOL ID
// ================================================================

private bool TryGetSchoolId(out Guid schoolId)
{
    schoolId = Guid.Empty;

    var schoolIdClaim =
        User.FindFirst("schoolId")?.Value;

    if (string.IsNullOrWhiteSpace(schoolIdClaim))
    {
        return false;
    }

    return Guid.TryParse(
        schoolIdClaim,
        out schoolId);
}


// ================================================================
// CREATE
// ================================================================

[HttpPost]
public async Task<IActionResult> Create(
    [FromBody] CreateAcademicTermRequest request)
{
    if (!TryGetSchoolId(out var schoolId))
    {
        return Unauthorized(new
        {
            success = false,
            error = "School information not found."
        });
    }

    var result =
        await _academicTermService.CreateAsync(
            schoolId,
            request);

    if (!result.Success)
    {
        return BadRequest(new
        {
            success = false,
            error = result.Error
        });
    }

    return Ok(new
    {
        success = true,
        data = result.Data
    });
}


// ================================================================
// GET ALL TERMS FOR SESSION
// ================================================================

[HttpGet("session/{academicSessionId:guid}")]
public async Task<IActionResult> GetAll(
    Guid academicSessionId)
{
    if (!TryGetSchoolId(out var schoolId))
    {
        return Unauthorized(new
        {
            success = false,
            error = "School information not found."
        });
    }

    var result =
        await _academicTermService.GetAllAsync(
            schoolId,
            academicSessionId);

    if (!result.Success)
    {
        return NotFound(new
        {
            success = false,
            error = result.Error
        });
    }

    return Ok(new
    {
        success = true,
        data = result.Data
    });
}


// ================================================================
// GET BY ID
// ================================================================

[HttpGet("{termId:guid}")]
public async Task<IActionResult> GetById(
    Guid termId)
{
    if (!TryGetSchoolId(out var schoolId))
    {
        return Unauthorized(new
        {
            success = false,
            error = "School information not found."
        });
    }

    var result =
        await _academicTermService.GetByIdAsync(
            schoolId,
            termId);

    if (!result.Success)
    {
        return NotFound(new
        {
            success = false,
            error = result.Error
        });
    }

    return Ok(new
    {
        success = true,
        data = result.Data
    });
}


// ================================================================
// UPDATE
// ================================================================

[HttpPut("{termId:guid}")]
public async Task<IActionResult> Update(
    Guid termId,
    [FromBody] UpdateAcademicTermRequest request)
{
    if (!TryGetSchoolId(out var schoolId))
    {
        return Unauthorized(new
        {
            success = false,
            error = "School information not found."
        });
    }

    var result =
        await _academicTermService.UpdateAsync(
            schoolId,
            termId,
            request);

    if (!result.Success)
    {
        return BadRequest(new
        {
            success = false,
            error = result.Error
        });
    }

    return Ok(new
    {
        success = true,
        data = result.Data
    });
}


// ================================================================
// DELETE
// ================================================================

[HttpDelete("{termId:guid}")]
public async Task<IActionResult> Delete(
    Guid termId)
{
    if (!TryGetSchoolId(out var schoolId))
    {
        return Unauthorized(new
        {
            success = false,
            error = "School information not found."
        });
    }

    var result =
        await _academicTermService.DeleteAsync(
            schoolId,
            termId);

    if (!result.Success)
    {
        return BadRequest(new
        {
            success = false,
            error = result.Error
        });
    }

    return Ok(new
    {
        success = true,
        message = "Academic term deleted successfully."
    });
}

}