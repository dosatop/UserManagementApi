using Microsoft.AspNetCore.Identity;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.Models;

namespace UserManagementApi.Services;

public class RolesService
(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;

    public async Task EnsureRolesExistAsync()
    {
        var roles = new[] { Roles.Admin, Roles.Student, Roles.Teacher };

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    public async Task AssignRoleToUserAsync(User user, string role)
    {
        if (!await _roleManager.RoleExistsAsync(role))
        {
            throw new InvalidOperationException($"Role '{role}' does not exist.");
        }

        var result = await _userManager.AddToRoleAsync(user, role);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to assign role '{role}' to user '{user.Email}': " +
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

}
