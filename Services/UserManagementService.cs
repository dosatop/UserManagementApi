using Microsoft.AspNetCore.Identity;
using UserManagementApi.Models;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

public class UserManagementService(
    UserManager<User> userManager) : IUserManagementService
{
    private readonly UserManager<User> _userManager = userManager;

    public async Task<(
        bool Success,
        User? User,
        string? Error
    )> CreateUserAsync(
        string fullName,
        string email,
        string password,
        string role)
    {
        // Check if email already exists
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
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = false
        };

        var createResult = await _userManager.CreateAsync(
            user,
            password);

        if (!createResult.Succeeded)
        {
            var errors = string.Join(
                ", ",
                createResult.Errors.Select(x => x.Description)
            );

            return (
                false,
                null,
                errors
            );
        }

        // Assign role
        var roleResult = await _userManager.AddToRoleAsync(
            user,
            role);

        if (!roleResult.Succeeded)
        {
            // Remove user if role assignment failed
            await _userManager.DeleteAsync(user);

            var errors = string.Join(
                ", ",
                roleResult.Errors.Select(x => x.Description)
            );

            return (
                false,
                null,
                $"User was created but role assignment failed: {errors}"
            );
        }

        return (
            true,
            user,
            null
        );
    }
}