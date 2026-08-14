using UserManagementApi.DTOs.Results;
using UserManagementApi.DTOs.TeacherPortal;

namespace UserManagementApi.Services.Interfaces;

public interface ITeacherPortalService
{
    // ================================================================
    // PROFILE
    // ================================================================

    Task<(bool Success, object? Data, string? Error)>
        GetProfileAsync(string userId);

    // ================================================================
    // CLASSES
    // ================================================================

    Task<(bool Success, object? Data, string? Error)>
        GetClassesAsync(string userId);

    // ================================================================
    // SUBJECTS
    // ================================================================

    Task<(bool Success, object? Data, string? Error)>
        GetSubjectsAsync(string userId);

    // ================================================================
    // RESULTS
    // ================================================================

    Task<(bool Success, object? Data, string? Error)>
        GetResultsAsync(
            Guid schoolId,
            Guid teacherId,
            GetTeacherResultsRequest request);

    Task<(bool Success, object? Data, string? Error)>
        CreateResultAsync(
            string userId,
            CreateResultRequest request);

    Task<(bool Success, object? Data, string? Error)>
        UpdateResultAsync(
            string userId,
            Guid resultId,
            CreateResultRequest request);

    Task<(bool Success, string? Error)>
        DeleteResultAsync(
            string userId,
            Guid resultId);

    // ================================================================
    // ASSIGNMENTS
    // ================================================================

    Task<(bool Success, object? Data, string? Error)>
        CreateAssignmentAsync(
            string userId,
            CreateAssignmentRequest request);

    Task<(bool Success, object? Data, string? Error)>
        GetAssignmentsAsync(
            string userId,
            GetTeacherAssignmentsRequest request);

    Task<(bool Success, object? Data, string? Error)>
        GetAssignmentAsync(
            string userId,
            Guid assignmentId);

    Task<(bool Success, object? Data, string? Error)>
        UpdateAssignmentAsync(
            string userId,
            Guid assignmentId,
            UpdateAssignmentRequest request);

    Task<(bool Success, string? Error)>
        DeleteAssignmentAsync(
            string userId,
            Guid assignmentId);

    // ================================================================
    // ATTENDANCE
    // ================================================================

    Task<(bool Success, object? Data, string? Error)>
        CreateAttendanceAsync(
            string userId,
            CreateAttendanceRequest request);

    Task<(bool Success, object? Data, string? Error)>
        GetAttendanceAsync(
            string userId,
            GetTeacherAttendanceRequest request);

    Task<(bool Success, object? Data, string? Error)>
        GetAttendanceRecordAsync(
            string userId,
            Guid attendanceId);

    Task<(bool Success, object? Data, string? Error)>
        UpdateAttendanceAsync(
            string userId,
            Guid attendanceId,
            UpdateAttendanceRequest request);

    Task<(bool Success, string? Error)>
        DeleteAttendanceAsync(
            string userId,
            Guid attendanceId);
}