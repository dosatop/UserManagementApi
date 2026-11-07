using UserManagementApi.DTOs.SuperAdmin;
using UserManagementApi.DTOs.Users;

namespace UserManagementApi.Services.Interfaces;

// ====================================================================
// SUPER ADMIN SERVICE
// ====================================================================
// Every method returns a SuperAdminResult:
//   Success  → Data holds the response body
//   NotFound → the school (or class, trade…) doesn't exist  → 404
//   otherwise → Error holds the message                      → 400

public record SuperAdminResult(
    bool Success,
    object? Data = null,
    string? Error = null,
    bool NotFound = false)
{
    public static SuperAdminResult Ok(object data) => new(true, data);

    public static SuperAdminResult Fail(string error) => new(false, null, error);

    public static SuperAdminResult Missing(string error) =>
        new(false, null, error, NotFound: true);
}

public interface ISuperAdminService
{
    // ---- School admins ----------------------------------------------
    Task<SuperAdminResult> CreateSchoolAdminAsync(
        Guid schoolId,
        CreateSchoolAdminRequest request);

    // ---- Teachers ---------------------------------------------------
    Task<SuperAdminResult> GetSchoolTeachersAsync(Guid schoolId);

    Task<SuperAdminResult> CreateSchoolTeacherAsync(
        Guid schoolId,
        SuperAdminCreateTeacherRequest request);

    // ---- Students ---------------------------------------------------
    Task<SuperAdminResult> GetSchoolStudentsAsync(
        Guid schoolId,
        Guid? classId = null);

    Task<SuperAdminResult> CreateSchoolStudentAsync(
        Guid schoolId,
        SuperAdminCreateStudentRequest request);

    // ---- Classes ----------------------------------------------------
    Task<SuperAdminResult> GetSchoolClassesAsync(Guid schoolId);

    Task<SuperAdminResult> CreateSchoolClassAsync(
        Guid schoolId,
        SuperAdminCreateClassRequest request);

    // ---- Form options (Add student form) ----------------------------
    // Departments, trades and each trade's subjects.
    Task<SuperAdminResult> GetSchoolFormOptionsAsync(Guid schoolId);
}