using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.Subjects;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Controllers;

[ApiController]
[Route("api/schools/{schoolId:guid}/subjects")]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
public class SubjectsController : ControllerBase
{
    private readonly ISubjectService _subjectService;

    public SubjectsController(
        ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateSubject(
        Guid schoolId,
        [FromBody] CreateSubjectRequest request)
    {
        var result =
            await _subjectService.CreateSubjectAsync(
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
                message = "Subject created successfully.",
                subject = result.Data
            });
    }

    [HttpGet]
    public async Task<IActionResult> GetSubjects(
        Guid schoolId)
    {
        var subjects =
            await _subjectService.GetSubjectsAsync(
                schoolId);

        return Ok(subjects);
    }

    [HttpGet("{subjectId:guid}")]
    public async Task<IActionResult> GetSubject(
        Guid schoolId,
        Guid subjectId)
    {
        var result =
            await _subjectService.GetSubjectAsync(
                schoolId,
                subjectId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }

    [HttpPut("{subjectId:guid}")]
    public async Task<IActionResult> UpdateSubject(
        Guid schoolId,
        Guid subjectId,
        [FromBody] CreateSubjectRequest request)
    {
        var result =
            await _subjectService.UpdateSubjectAsync(
                schoolId,
                subjectId,
                request);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Subject updated successfully.",
            subject = result.Data
        });
    }

    [HttpDelete("{subjectId:guid}")]
    public async Task<IActionResult> DeleteSubject(
        Guid schoolId,
        Guid subjectId)
    {
        var result =
            await _subjectService.DeleteSubjectAsync(
                schoolId,
                subjectId);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Subject deleted successfully."
        });
    }
}