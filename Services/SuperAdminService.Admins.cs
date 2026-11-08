using Microsoft.EntityFrameworkCore;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.DTOs.SuperAdmin;
using UserManagementApi.Models;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

// ====================================================================
// SUPER ADMIN SERVICE — SCHOOL ADMINS (list / edit / delete)
// ====================================================================
// A school admin is a User in the "Admin" role whose SchoolId is the
// school. Creating one still goes through CreateSchoolAdminAsync.

public partial class SuperAdminService
{
    // ================================================================
    // LIST
    // ================================================================

    public async Task<SuperAdminResult> GetSchoolAdminsAsync(Guid schoolId)
    {
        if (!await SchoolExists(schoolId))
            return SuperAdminResult.Missing("School not found.");

        var admins = (await _userManager.GetUsersInRoleAsync(Roles.Admin))
            .Where(u => u.SchoolId == schoolId)
            .OrderBy(u => u.FullName)
            .Select(u => new
            {
                id = u.Id,
                fullName = u.FullName,
                email = u.Email,
                phoneNumber = u.PhoneNumber,
                schoolId = u.SchoolId,
                isLockedOut = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow
            })
            .ToList();

        return SuperAdminResult.Ok(new { adminCount = admins.Count, admins });
    }

    // ================================================================
    // UPDATE
    // ================================================================

    public async Task<SuperAdminResult> UpdateSchoolAdminAsync(
        Guid schoolId,
        string adminId,
        SuperAdminUpdateTeacherRequest request)   // same fields: name, email, phone, new password
    {
        var (admin, missing) = await FindSchoolAdminAsync(schoolId, adminId);
        if (admin == null) return SuperAdminResult.Missing(missing!);

        var error = await UpdateUserAsync(
            admin,
            request.FullName,
            request.Email,
            request.PhoneNumber,
            request.NewPassword);

        if (error != null) return SuperAdminResult.Fail(error);

        return SuperAdminResult.Ok(new
        {
            message = "School admin updated successfully.",
            admin = new
            {
                id = admin.Id,
                fullName = admin.FullName,
                email = admin.Email,
                phoneNumber = admin.PhoneNumber,
                schoolId = admin.SchoolId
            }
        });
    }

    // ================================================================
    // DELETE
    // ================================================================
    // Deletes the admin's login. Allowed even for the last admin (the
    // super admin can create a new one), but the app warns first.

    public async Task<SuperAdminResult> DeleteSchoolAdminAsync(
        Guid schoolId,
        string adminId)
    {
        var (admin, missing) = await FindSchoolAdminAsync(schoolId, adminId);
        if (admin == null) return SuperAdminResult.Missing(missing!);

        try
        {
            var result = await _userManager.DeleteAsync(admin);

            if (!result.Succeeded)
                return SuperAdminResult.Fail(IdentityError(result));

            return SuperAdminResult.Ok(new { message = "School admin deleted successfully." });
        }
        catch (DbUpdateException)
        {
            return SuperAdminResult.Fail(
                "This admin still has records linked to their account " +
                "(for example results or sessions they created), so they " +
                "can't be deleted.");
        }
    }

    // ================================================================
    // HELPER
    // ================================================================

    private async Task<(User? Admin, string? Error)> FindSchoolAdminAsync(
        Guid schoolId,
        string adminId)
    {
        var user = await _userManager.FindByIdAsync(adminId);

        if (user == null || user.SchoolId != schoolId)
            return (null, "Admin not found in this school.");

        if (!await _userManager.IsInRoleAsync(user, Roles.Admin))
            return (null, "This user is not a school admin.");

        return (user, null);
    }
}
