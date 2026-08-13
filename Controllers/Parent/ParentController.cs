using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.Parents;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Controllers;

[ApiController]
[Route("api/schools/{schoolId:guid}/parents")]
[Authorize(Roles = Roles.Admin)]
public class ParentsController(
    IParentService parentService) : ControllerBase
{
    private readonly IParentService _parentService = parentService;

    [HttpPost]
    public async Task<IActionResult> CreateParent(
        Guid schoolId,
        [FromBody] CreateParentRequest request)
    {
        var result =
            await _parentService.CreateParentAsync(
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
                message = "Parent created successfully.",
                parent = result.Data
            });
    }

    [HttpGet]
    public async Task<IActionResult> GetParents(
        Guid schoolId)
    {
        var parents =
            await _parentService.GetParentsAsync(
                schoolId);

        return Ok(parents);
    }

    [HttpPost("{parentId:guid}/students/{studentId:guid}")]
    public async Task<IActionResult> AssignStudent(
        Guid schoolId,
        Guid parentId,
        Guid studentId)
    {
        var result =
            await _parentService.AssignStudentAsync(
                schoolId,
                parentId,
                studentId);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Student linked to parent successfully.",
            relationship = result.Data
        });
    }

    [HttpDelete("{parentId:guid}/students/{studentId:guid}")]
    public async Task<IActionResult> RemoveStudent(
        Guid schoolId,
        Guid parentId,
        Guid studentId)
    {
        var result =
            await _parentService.RemoveStudentAsync(
                schoolId,
                parentId,
                studentId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Student removed from parent."
        });
    }
}