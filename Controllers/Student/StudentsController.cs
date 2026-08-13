using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementApi.DTOs.Auth.Roles;

[ApiController]
[Route("api/schools/{schoolId:guid}/students")]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentsController(
        IStudentService studentService)
    {
        _studentService = studentService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateStudent(
        Guid schoolId,
        CreateStudentRequest request)
    {
        var result =
            await _studentService.CreateStudentAsync(
                schoolId,
                request);

        if (!result.Success)
        {
            return BadRequest(result.Error);
        }

        return Ok(new
        {
            message = "Student created successfully.",
            student = result.Data
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetStudents(
        Guid schoolId)
    {
        return Ok(
            await _studentService.GetStudentsAsync(
                schoolId));
    }
}