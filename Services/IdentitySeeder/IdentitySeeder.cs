using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using UserManagementApi.Configuration;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.Models;

namespace UserManagementApi.Services;

public static class IdentitySeeder
{
    public static async Task SeedAsync(
        IServiceProvider services)
    {
        var roleManager =
            services.GetRequiredService<
                RoleManager<IdentityRole>>();

        var userManager =
            services.GetRequiredService<
                UserManager<User>>();

        var adminSettings =
            services.GetRequiredService<
                IOptions<SeedAdminSettings>>().Value;

        // Create roles
        string[] roles =
        {
            Roles.Admin,
            Roles.Teacher,
            Roles.Student,
            Roles.Parent
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var roleResult =
                    await roleManager.CreateAsync(
                        new IdentityRole(role));

                if (!roleResult.Succeeded)
                {
                    throw new Exception(
                        $"Failed to create role {role}: " +
                        string.Join(
                            ", ",
                            roleResult.Errors.Select(
                                x => x.Description)));
                }
            }
        }

        // Find admin
        var admin =
            await userManager.FindByEmailAsync(
                adminSettings.Email);

        // Create admin if it doesn't exist
        if (admin == null)
        {
            admin = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = adminSettings.Email,
                Email = adminSettings.Email,
                FullName = adminSettings.FullName,
                EmailConfirmed = true
            };

            var result =
                await userManager.CreateAsync(
                    admin,
                    adminSettings.Password);

            if (!result.Succeeded)
            {
                throw new Exception(
                    "Failed to create Super Admin: " +
                    string.Join(
                        ", ",
                        result.Errors.Select(
                            x => x.Description)));
            }
        }

        // Make sure admin has Admin role
        if (!await userManager.IsInRoleAsync(
                admin,
                Roles.Admin))
        {
            var result =
                await userManager.AddToRoleAsync(
                    admin,
                    Roles.Admin);

            if (!result.Succeeded)
            {
                throw new Exception(
                    "Failed to assign Admin role: " +
                    string.Join(
                        ", ",
                        result.Errors.Select(
                            x => x.Description)));
            }
        }
    }
}