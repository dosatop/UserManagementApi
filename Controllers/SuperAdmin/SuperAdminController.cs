using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.SuperAdmin;
using UserManagementApi.DTOs.Users;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Controllers;

// ====================================================================
// SUPER ADMIN CONTROLLER
// ====================================================================
//   POST api/super-admin/schools/{schoolId}/admin
//
//   GET  api/super-admin/schools/{schoolId}/teachers
//   POST api/super-admin/schools/{schoolId}/teachers
//
//   GET  api/super-admin/schools/{schoolId}/students   (?classId=)
//   POST api/super-admin/schools/{schoolId}/students
//
//   GET  api/super-admin/schools/{schoolId}/classes
//   POST api/super-admin/schools/{schoolId}/classes
//
//   GET  api/super-admin/schools/{schoolId}/options    (departments + trades)
//
// Thin: all business logic lives in SuperAdminService.

[ApiController]
[Route("api/super-admin/schools/{schoolId:guid}")]
[Authorize(Roles = Roles.SuperAdmin)]
public class SuperAdminController(ISuperAdminService superAdminService)
    : ControllerBase
{
    private readonly ISuperAdminService _service = superAdminService;

    // ================================================================
    // SCHOOL ADMINS
    // ================================================================

    [HttpPost("admin")]
    public async Task<IActionResult> CreateSchoolAdmin(
        Guid schoolId,
        CreateSchoolAdminRequest request) =>
        ToResponse(await _service.CreateSchoolAdminAsync(schoolId, request));

    // ================================================================
    // TEACHERS
    // ================================================================

    [HttpGet("teachers")]
    public async Task<IActionResult> GetSchoolTeachers(Guid schoolId) =>
        ToResponse(await _service.GetSchoolTeachersAsync(schoolId));

    [HttpPost("teachers")]
    public async Task<IActionResult> CreateSchoolTeacher(
        Guid schoolId,
        SuperAdminCreateTeacherRequest request) =>
        ToResponse(await _service.CreateSchoolTeacherAsync(schoolId, request));

    // ================================================================
    // STUDENTS
    // ================================================================

    [HttpGet("students")]
    public async Task<IActionResult> GetSchoolStudents(
        Guid schoolId,
        [FromQuery] Guid? classId = null) =>
        ToResponse(await _service.GetSchoolStudentsAsync(schoolId, classId));

    [HttpPost("students")]
    public async Task<IActionResult> CreateSchoolStudent(
        Guid schoolId,
        SuperAdminCreateStudentRequest request) =>
        ToResponse(await _service.CreateSchoolStudentAsync(schoolId, request));

    // ================================================================
    // CLASSES
    // ================================================================

    [HttpGet("classes")]
    public async Task<IActionResult> GetSchoolClasses(Guid schoolId) =>
        ToResponse(await _service.GetSchoolClassesAsync(schoolId));

    [HttpPost("classes")]
    public async Task<IActionResult> CreateSchoolClass(
        Guid schoolId,
        SuperAdminCreateClassRequest request) =>
        ToResponse(await _service.CreateSchoolClassAsync(schoolId, request));

    // ================================================================
    // FORM OPTIONS (Add student form)
    // ================================================================

    [HttpGet("options")]
    public async Task<IActionResult> GetSchoolFormOptions(Guid schoolId) =>
        ToResponse(await _service.GetSchoolFormOptionsAsync(schoolId));

    // ================================================================
    // RESULT → HTTP
    // ================================================================
    // [ApiController] already returns 400 for invalid models, so no
    // ModelState checks are needed in the actions.

    private IActionResult ToResponse(SuperAdminResult result)
    {
        if (result.Success) return Ok(result.Data);

        if (result.NotFound) return NotFound(new { message = result.Error });

        return BadRequest(new { message = result.Error });
    }
}