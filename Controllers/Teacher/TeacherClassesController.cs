using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.Teachers;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Controllers;

[ApiController]
[Route(
    "api/schools/{schoolId:guid}/teachers/{teacherId:guid}/classes"
)]
[Authorize(Roles = Roles.Admin)]
public class TeacherClassesController : ControllerBase
{
    private readonly ITeacherClassService _teacherClassService;

    public TeacherClassesController(
        ITeacherClassService teacherClassService)
    {
        _teacherClassService = teacherClassService;
    }

    // POST
    // Assign teacher to class
    [HttpPost]
    public async Task<IActionResult> AssignClass(
        Guid schoolId,
        Guid teacherId,
        [FromBody] AssignTeacherClassRequest request)
    {
        var result =
            await _teacherClassService.AssignClassAsync(
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
                message = "Teacher assigned to class successfully.",
                assignment = result.Data
            });
    }

    // GET
    // Get classes assigned to teacher
    [HttpGet]
    public async Task<IActionResult> GetTeacherClasses(
        Guid schoolId,
        Guid teacherId)
    {
        var classes =
            await _teacherClassService.GetTeacherClassesAsync(
                schoolId,
                teacherId);

        return Ok(classes);
    }

    // DELETE
    // Remove teacher from class
    [HttpDelete("{classId:guid}")]
    public async Task<IActionResult> RemoveClass(
        Guid schoolId,
        Guid teacherId,
        Guid classId)
    {
        var result =
            await _teacherClassService.RemoveClassAsync(
                schoolId,
                teacherId,
                classId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Teacher removed from class successfully."
        });
    }
}