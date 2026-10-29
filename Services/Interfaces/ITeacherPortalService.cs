using UserManagementApi.DTOs.Attendance;
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
    // STUDENTS
    // ================================================================

    Task<(bool Success, object? Data, string? Error)>
        GetStudentsAsync(
            string userId,
            Guid? classId = null);

    Task<(bool Success, object? Data, string? Error)>
        GetStudentDetailsAsync(
            string userId,
            Guid studentId);

    // ================================================================
    // RESULTS
    // ================================================================

    Task<(bool Success, object? Data, string? Error)>
        GetResultsAsync(
            string userId,
            GetTeacherResultsRequest request);

    Task<(bool Success, object? Data, string? Error)>
        UploadResultAsync(
            string userId,
            UploadResultRequest request);

    Task<(bool Success, object? Data, string? Error)>
        UploadTestResultAsync(
            string userId,
            UploadTestResultRequest request);

    Task<(bool Success, object? Data, string? Error)>
        UploadExamResultAsync(
            string userId,
            UploadExamResultRequest request);

    Task<(bool Success, object? Data, string? Error)>
        BulkResultAsync(
            string userId,
            BulkResultRequest request);

    Task<(bool Success, object? Data, string? Error)>
        BulkTestResultAsync(
            string userId,
            BulkTestResultRequest request);

    Task<(bool Success, object? Data, string? Error)>
        BulkExamResultAsync(
            string userId,
            BulkExamResultRequest request);

    Task<(bool Success, object? Data, string? Error)>
        UpdateResultAsync(
            string userId,
            Guid resultId,
            UpdateResultRequest request);

    Task<(bool Success, object? Data, string? Error)>
        UpdateTestResultAsync(
            string userId,
            Guid resultId,
            UpdateTestResultRequest request);

    Task<(bool Success, object? Data, string? Error)>
        UpdateExamResultAsync(
            string userId,
            Guid resultId,
            UpdateExamResultRequest request);

    Task<(bool Success, object? Data, string? Error)>
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
        CreateBulkAttendanceAsync(
            string userId,
            CreateBulkAttendanceRequest request);

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