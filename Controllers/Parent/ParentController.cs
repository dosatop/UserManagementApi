using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.Parents;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Controllers;

[ApiController]
[Route("api/schools/{schoolId:guid}/parents")]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
public class ParentsController(
    IParentService parentService) : ControllerBase
{
    private readonly IParentService _parentService = parentService;

    [HttpPost]
    public async Task<IActionResult> CreateParent(
        Guid schoolId,
        [FromBody] CreateParentRequest request)
    {
        try
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
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "An unexpected error occurred.",
                    error = ex.Message
                });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetParents(
        Guid schoolId)
    {
        try
        {
            var parents =
                await _parentService.GetParentsAsync(
                    schoolId);

            return Ok(parents);
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "An unexpected error occurred.",
                    error = ex.Message
                });
        }
    }

    [HttpPost("{parentId:guid}/students/{studentId:guid}")]
    public async Task<IActionResult> AssignStudent(
        Guid schoolId,
        Guid parentId,
        Guid studentId)
    {
        try
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
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "An unexpected error occurred.",
                    error = ex.Message
                });
        }
    }

    [HttpDelete("{parentId:guid}/students/{studentId:guid}")]
    public async Task<IActionResult> RemoveStudent(
        Guid schoolId,
        Guid parentId,
        Guid studentId)
    {
        try
        {
            var (Success, Error) =
                await _parentService.RemoveStudentAsync(
                    schoolId,
                    parentId,
                    studentId);

            if (!Success)
            {
                return NotFound(new
                {
                    message = Error
                });
            }

            return Ok(new
            {
                message = "Student removed from parent."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "An unexpected error occurred.",
                    error = ex.Message
                });
        }
    }
}