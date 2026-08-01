using Microsoft.AspNetCore.Identity;
using UserManagementApi.DTOs.User;
using UserManagementApi.Models;

namespace UserManagementApi.Services;

public class UserService(UserManager<User> userManager)
{
    private readonly UserManager<User> _userManager = userManager;

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