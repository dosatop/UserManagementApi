using UserManagementApi.DTOs.SuperAdmin;
using UserManagementApi.DTOs.Subjects;
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

    // ---- Edit / delete (SuperAdminService.Manage.cs) ----------------
    Task<SuperAdminResult> UpdateSchoolTeacherAsync(
        Guid schoolId, Guid teacherId, SuperAdminUpdateTeacherRequest request);

    Task<SuperAdminResult> DeleteSchoolTeacherAsync(Guid schoolId, Guid teacherId);

    Task<SuperAdminResult> UpdateSchoolStudentAsync(
        Guid schoolId, Guid studentId, SuperAdminUpdateStudentRequest request);

    Task<SuperAdminResult> DeleteSchoolStudentAsync(Guid schoolId, Guid studentId);

    Task<SuperAdminResult> UpdateSchoolClassAsync(
        Guid schoolId, Guid classId, SuperAdminUpdateClassRequest request);

    Task<SuperAdminResult> DeleteSchoolClassAsync(Guid schoolId, Guid classId);

    Task<SuperAdminResult> RemoveClassTeacherAsync(Guid schoolId, Guid classId);

    // ---- Subjects (SuperAdminService.Subjects.cs) -------------------
    Task<SuperAdminResult> GetSchoolSubjectsAsync(Guid schoolId);

    Task<SuperAdminResult> CreateSchoolSubjectAsync(
        Guid schoolId, CreateSubjectRequest request);

    Task<SuperAdminResult> UpdateSchoolSubjectAsync(
        Guid schoolId, Guid subjectId, CreateSubjectRequest request);

    Task<SuperAdminResult> DeleteSchoolSubjectAsync(Guid schoolId, Guid subjectId);

    Task<SuperAdminResult> AssignSchoolSubjectAsync(
        Guid schoolId, Guid subjectId, SuperAdminAssignSubjectRequest request);

    Task<SuperAdminResult> RemoveSubjectFromClassAsync(
        Guid schoolId, Guid subjectId, Guid classId);

    // ---- School admins (SuperAdminService.Admins.cs) -----------------
    Task<SuperAdminResult> GetSchoolAdminsAsync(Guid schoolId);

    Task<SuperAdminResult> UpdateSchoolAdminAsync(
        Guid schoolId, string adminId, SuperAdminUpdateTeacherRequest request);

    Task<SuperAdminResult> DeleteSchoolAdminAsync(Guid schoolId, string adminId);
}