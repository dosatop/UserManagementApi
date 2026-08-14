using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.Results;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Controllers;

[ApiController]
[Route("api/schools/{schoolId:guid}/results")]
[Authorize(Roles = Roles.Admin)]
public class ResultsController : ControllerBase
{

    private readonly IResultImportService _resultImportService;

    private Guid? GetSchoolId()
    {
        var value = User.FindFirst("SchoolId")?.Value;

        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Guid.TryParse(value, out var schoolId)
            ? schoolId
            : null;
    }

    [HttpPost("manual")]
    public async Task<IActionResult> CreateResult(
    CreateResultRequest request)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _resultImportService.CreateAsync(
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
    [HttpGet]
    public async Task<IActionResult> GetResults(
        [FromQuery] GetResultsRequest request)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _resultImportService.GetAsync(
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

    [HttpPut("{resultId:guid}")]
    public async Task<IActionResult> UpdateResult(
    Guid resultId,
    UpdateResultRequest request)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _resultImportService.UpdateAsync(
            schoolId.Value,
            resultId,
            request);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }

    [HttpDelete("{resultId:guid}")]
    public async Task<IActionResult> DeleteResult(Guid resultId)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _resultImportService.DeleteAsync(
            schoolId.Value,
            resultId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Result deleted successfully."
        });
    }


    public ResultsController(
        IResultImportService resultImportService)
    {
        _resultImportService = resultImportService;
    }

    [HttpPost("import/preview")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> PreviewImport(
        Guid schoolId,
        [FromForm] ImportResultsRequest request)
    {
        var result =
            await _resultImportService.PreviewAsync(
                schoolId,
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

    [HttpPost("import/confirm")]
    public async Task<IActionResult> ConfirmImport(
    Guid schoolId,
    [FromBody] ConfirmResultImportRequest request)
    {
        var result =
            await _resultImportService.ConfirmImportAsync(
                schoolId,
                request);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return StatusCode(
            StatusCodes.Status201Created,
            new
            {
                message = "Results imported successfully.",
                result = result.Data
            });
    }
}