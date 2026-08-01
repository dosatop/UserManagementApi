using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementApi.Services;

namespace UserManagementApi.Controllers;

[ApiController]
[Route("api/user")]
public class UserController(
UserService userService
) : ControllerBase
{
    private readonly UserService _userService = userService;

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetUserProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
        {
            return Unauthorized();
        }

        var userProfile = await _userService.GetUserProfileAsync(userId);

        if (userProfile is null)
        {
            return NotFound();
        }

        return Ok(userProfile);
    }
}