using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.Teachers;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Controllers;

[ApiController]
[Route("api/schools/{schoolId:guid}/teachers")]
[Authorize(Roles = Roles.Admin)]
public class TeachersController : ControllerBase
{
    private readonly ITeacherService _teacherService;

    public TeachersController(ITeacherService teacherService)
    {
        _teacherService = teacherService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTeacher(
        Guid schoolId,
        [FromBody] CreateTeacherRequest request)
    {
        var result = await _teacherService.CreateTeacherAsync(
            schoolId,
            request);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            message = "Teacher created successfully.",
            teacher = result.Data
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetTeachers(Guid schoolId)
    {
        var teachers = await _teacherService.GetTeachersAsync(schoolId);

        return Ok(teachers);
    }
}