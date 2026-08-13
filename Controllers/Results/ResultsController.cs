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