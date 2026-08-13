using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using UserManagementApi.DTOs.User;
using UserManagementApi.Models;

namespace UserManagementApi.Services;

public interface ICurrentUserService
{
    string? GetUserId();
    string? GetEmail();
    string? GetUsername();
}

public class UserService(UserManager<User> userManager, IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public async Task<(bool Success, User? User, string? Error)>
      CreateUserAsync(
          string fullName,
          string email,
          string password,
          string role)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser != null)
        {
            return (
                false,
                null,
                "A user with this email already exists."
            );
        }

        var user = new User
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(
            user,
            password);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                ", ",
                result.Errors.Select(x => x.Description));

            return (
                false,
                null,
                errors
            );
        }

        var roleResult = await _userManager.AddToRoleAsync(
            user,
            role);

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            var errors = string.Join(
                ", ",
                roleResult.Errors.Select(x => x.Description));

            return (
                false,
                null,
                errors
            );
        }

        return (
            true,
            user,
            null
        );
    }

    public string? GetUserId()
    {
        return _httpContextAccessor.HttpContext?
            .User
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;
    }

    public string? GetEmail()
    {
        return _httpContextAccessor.HttpContext?
            .User
            .FindFirst(ClaimTypes.Email)?
            .Value;
    }

    public string? GetUsername()
    {
        return _httpContextAccessor.HttpContext?
            .User
            .FindFirst(ClaimTypes.Name)?
            .Value;
    }

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        return await _userManager.FindByIdAsync(userId);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<UserProfileResponse?> GetUserProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        return new UserProfileResponse
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            UserName = user.UserName
        };

    }



}