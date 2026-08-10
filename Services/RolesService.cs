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

    public async Task<IdentityResult> AssignRoleToUserAsync(User user, string role)
    {
        if (!await _roleManager.RoleExistsAsync(role))
        {
            return IdentityResult.Failed(new IdentityError { Description = $"Role '{role}' does not exist." });
        }

        var result = await _userManager.AddToRoleAsync(user, role);

        if (!result.Succeeded)
        {
            return IdentityResult.Failed([.. result.Errors]);
        }

        return IdentityResult.Success;
    }

}
