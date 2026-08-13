using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.Classes;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Controllers;

[ApiController]
[Route("api/schools/{schoolId:guid}/classes")]
[Authorize(Roles = Roles.Admin)]
public class ClassesController(IClassService classService) : ControllerBase
{
    private readonly IClassService _classService = classService;

    [HttpPost]
    public async Task<IActionResult> CreateClass(
        Guid schoolId,
        [FromBody] CreateClassRequest request)
    {
        var result = await _classService.CreateClassAsync(
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
                message = "Class created successfully.",
                classData = result.Data
            });
    }

    [HttpGet]
    public async Task<IActionResult> GetClasses(
        Guid schoolId)
    {
        var classes =
            await _classService.GetClassesAsync(schoolId);

        return Ok(classes);
    }

    [HttpGet("{classId:guid}")]
    public async Task<IActionResult> GetClass(
        Guid schoolId,
        Guid classId)
    {
        var result =
            await _classService.GetClassAsync(
                schoolId,
                classId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }

    [HttpPut("{classId:guid}")]
    public async Task<IActionResult> UpdateClass(
        Guid schoolId,
        Guid classId,
        [FromBody] CreateClassRequest request)
    {
        var result =
            await _classService.UpdateClassAsync(
                schoolId,
                classId,
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
            message = "Class updated successfully.",
            classData = result.Data
        });
    }

    [HttpDelete("{classId:guid}")]
    public async Task<IActionResult> DeleteClass(
        Guid schoolId,
        Guid classId)
    {
        var result =
            await _classService.DeleteClassAsync(
                schoolId,
                classId);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Class deleted successfully."
        });
    }
}