using Microsoft.AspNetCore.Mvc;
using UserManagementApi.DTOs.Auth;
using UserManagementApi.Services;

namespace UserManagementApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    AuthService authService) : ControllerBase
{
    private readonly AuthService _authService = authService;

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(
            request.Email,
            request.Password);

        if (!result.Succeeded)
        {
            return Unauthorized(new
            {
                errors = result.Errors
            });
        }

        return Ok(result.Data);
    }
}