namespace UserManagementApi.Services.Interfaces;

public interface IStudentPortalService
{
    Task<(bool Success, object? Data, string? Error)>
        GetProfileAsync(string userId);

    Task<(bool Success, object? Data, string? Error)>
        GetClassAsync(string userId);

    Task<(bool Success, object? Data, string? Error)>
        GetSubjectsAsync(string userId);
    Task<(bool Success, object? Data, string? Error)>
        GetResultsAsync(
            string userId,
            string session,
            string term);

    // ============================================================
    // ATTENDANCE
    // ============================================================

    Task<(bool Success, object? Data, string? Error)>
        GetAttendanceAsync(
            string userId,
            string session,
            string term);

    // ============================================================
    // ASSIGNMENTS
    // ============================================================

    Task<(bool Success, object? Data, string? Error)>
        GetAssignmentsAsync(
            string userId,
            string session,
            string term);

    Task<(bool Success, object? Data, string? Error)>
        GetAssignmentAsync(
            string userId,
            Guid assignmentId);

    Task<(bool Success, object? Data, string? Error)>
        SubmitAssignmentAsync(
            string userId,
            Guid assignmentId,
            string? submissionText,
            string? attachmentUrl);
}