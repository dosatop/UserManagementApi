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

    protected Guid? GetSchoolId()
    {
        var value = User.FindFirst("SchoolId")?.Value;

        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Guid.TryParse(value, out var schoolId)
            ? schoolId
            : null;
    }



    // ================================================================
    // CREATE
    // ================================================================

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAcademicTermRequest request)
    {
        var schoolId = GetSchoolId();

        if (!schoolId.HasValue)
        {
            return Unauthorized();
        }

        var result =
            await _academicTermService.CreateAsync(
                schoolId.Value,
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
        var schoolId = GetSchoolId();

        if (!schoolId.HasValue)
        {
            return Unauthorized();
        }


        var result =
            await _academicTermService.GetAllAsync(
                schoolId.Value,
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
        var schoolId = GetSchoolId();

        if (!schoolId.HasValue)
        {
            return Unauthorized();
        }


        var result =
            await _academicTermService.GetByIdAsync(
                schoolId.Value,
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
        var schoolId = GetSchoolId();

        if (!schoolId.HasValue)
        {
            return Unauthorized();
        }

        var result =
            await _academicTermService.UpdateAsync(
                schoolId.Value,
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
        var schoolId = GetSchoolId();

        if (!schoolId.HasValue)
        {
            return Unauthorized();
        }

        var result =
            await _academicTermService.DeleteAsync(
                schoolId.Value,
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