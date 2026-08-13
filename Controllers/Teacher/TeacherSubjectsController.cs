using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.Teachers;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Controllers;

[ApiController]
[Route(
    "api/schools/{schoolId:guid}/teachers/{teacherId:guid}/subjects"
)]
[Authorize(Roles = Roles.Admin)]
public class TeacherSubjectsController : ControllerBase
{
    private readonly ITeacherSubjectService _service;

    public TeacherSubjectsController(
        ITeacherSubjectService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> AssignSubject(
        Guid schoolId,
        Guid teacherId,
        [FromBody] AssignTeacherSubjectRequest request)
    {
        var result = await _service.AssignSubjectAsync(
            schoolId,
            teacherId,
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
                message = "Subject assigned to teacher successfully.",
                assignment = result.Data
            });
    }

    [HttpGet]
    public async Task<IActionResult> GetTeacherSubjects(
        Guid schoolId,
        Guid teacherId)
    {
        var subjects = await _service.GetTeacherSubjectsAsync(
            schoolId,
            teacherId);

        return Ok(subjects);
    }

    [HttpDelete("{subjectId:guid}")]
    public async Task<IActionResult> RemoveSubject(
        Guid schoolId,
        Guid teacherId,
        Guid subjectId)
    {
        var result = await _service.RemoveSubjectAsync(
            schoolId,
            teacherId,
            subjectId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Subject removed from teacher successfully."
        });
    }
}