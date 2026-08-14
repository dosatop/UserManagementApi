namespace UserManagementApi.Services.Interfaces;

public interface IParentPortalService
{
    // ============================================================
    // PARENT PROFILE
    // ============================================================

    Task<(bool Success, object? Data, string? Error)>
        GetProfileAsync(
            string userId);

    // ============================================================
    // CHILDREN
    // ============================================================

    Task<(bool Success, object? Data, string? Error)>
        GetChildrenAsync(
            string userId);

    // ============================================================
    // SINGLE CHILD
    // ============================================================

    Task<(bool Success, object? Data, string? Error)>
        GetChildAsync(
            string userId,
            Guid studentId);

    // ============================================================
    // CHILD CLASS
    // ============================================================

    Task<(bool Success, object? Data, string? Error)>
        GetChildClassAsync(
            string userId,
            Guid studentId);

    // ============================================================
    // CHILD SUBJECTS
    // ============================================================

    Task<(bool Success, object? Data, string? Error)>
        GetChildSubjectsAsync(
            string userId,
            Guid studentId);

    // ============================================================
    // CHILD RESULTS
    // ============================================================

    Task<(bool Success, object? Data, string? Error)>
        GetChildResultsAsync(
            string userId,
            Guid studentId,
            string session,
            string term);

    // ============================================================
    // CHILD ATTENDANCE
    // ============================================================

    Task<(bool Success, object? Data, string? Error)>
        GetChildAttendanceAsync(
            string userId,
            Guid studentId,
            string session,
            string term);

    // ============================================================
    // CHILD ASSIGNMENTS
    // ============================================================

    Task<(bool Success, object? Data, string? Error)>
        GetChildAssignmentsAsync(
            string userId,
            Guid studentId,
            string session,
            string term);

    // ============================================================
    // SINGLE CHILD ASSIGNMENT
    // ============================================================

    Task<(bool Success, object? Data, string? Error)>
        GetChildAssignmentAsync(
            string userId,
            Guid studentId,
            Guid assignmentId);
}