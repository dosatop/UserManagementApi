using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementApi.DTOs.Departments;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Controllers;

[ApiController]
[Route("api/schools/{schoolId}/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(
        IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    // ============================================================
    // CREATE
    // ============================================================

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid schoolId,
        CreateDepartmentRequest request)
    {
        var result =
            await _departmentService.CreateDepartmentAsync(
                schoolId,
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
            message = "Department created successfully.",
            data = result.Data
        });
    }

    // ============================================================
    // GET ALL
    // ============================================================

    [HttpGet]
    public async Task<IActionResult> GetAll(
        Guid schoolId)
    {
        var departments =
            await _departmentService.GetDepartmentsAsync(
                schoolId);

        return Ok(new
        {
            data = departments
        });
    }

    // ============================================================
    // GET ONE
    // ============================================================

    [HttpGet("{departmentId}")]
    public async Task<IActionResult> GetOne(
        Guid schoolId,
        Guid departmentId)
    {
        var result =
            await _departmentService.GetDepartmentAsync(
                schoolId,
                departmentId);

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            data = result.Data
        });
    }

    // ============================================================
    // UPDATE
    // ============================================================

    [HttpPut("{departmentId}")]
    public async Task<IActionResult> Update(
        Guid schoolId,
        Guid departmentId,
        CreateDepartmentRequest request)
    {
        var result =
            await _departmentService.UpdateDepartmentAsync(
                schoolId,
                departmentId,
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
            message = "Department updated successfully.",
            data = result.Data
        });
    }

    // ============================================================
    // DELETE
    // ============================================================

    [HttpDelete("{departmentId}")]
    public async Task<IActionResult> Delete(
        Guid schoolId,
        Guid departmentId)
    {
        var result =
            await _departmentService.DeleteDepartmentAsync(
                schoolId,
                departmentId);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Department deleted successfully."
        });
    }
}
