using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.Users;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Controllers;

[ApiController]
[Route("api/super-admin")]
[Authorize(Roles = Roles.SuperAdmin)]
public class SuperAdminController(
    IUserManagementService userManagementService)
    : ControllerBase
{
    private readonly IUserManagementService
        _userManagementService = userManagementService;

    [HttpPost("schools/{schoolId:guid}/admin")]
    public async Task<IActionResult> CreateSchoolAdmin(
     Guid schoolId,
     CreateSchoolAdminRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result =
            await _userManagementService.CreateSchoolAdminAsync(
                schoolId,
                request.FullName,
                request.Email,
                request.PhoneNumber,
                request.Password);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "School admin created successfully.",
            admin = new
            {
                id = result.User!.Id,
                fullName = result.User.FullName,
                email = result.User.Email,
                phoneNumber = result.User.PhoneNumber,
                schoolId = result.User.SchoolId,
                role = Roles.Admin
            }
        });
    }
}