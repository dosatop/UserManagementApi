using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UserManagementApi.DTOs.Auth;
using UserManagementApi.Models;
using UserManagementApi.Services;

namespace UserManagementApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<User> userManager,
    TokenService tokenService, AuthService authService) : ControllerBase
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly TokenService _tokenService = tokenService;
    private readonly AuthService _authService = authService;

    [HttpPost("signUp")]
    public async Task<IActionResult> SignUp(
        SignUpRequest request)
    {
        var result = await _authService.CreateUserAsync(
            request.Email,
            request.Password,
            request.FullName,
            request.PhoneNumber);

        return Ok(new
        {
            message = "User created successfully"
        });
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request)
    {
        var loginResult = await _authService.LoginAsync(request.Email, request.Password);

        if (!loginResult.Succeeded)
        {
            return Unauthorized(new
            {
                errors = loginResult.Errors
            });
        }

        return Ok(loginResult.Data);
    }
}