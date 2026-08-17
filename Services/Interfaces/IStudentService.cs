using UserManagementApi.DTOs.Students;

namespace UserManagementApi.Services.Interfaces;

public interface IStudentService
{
    Task<(bool Success, object? Data, string? Error)>
        CreateStudentAsync(
            Guid schoolId,
            CreateStudentRequest request);

   Task<(bool Success, object? Data, string? Error)>
    GetStudentsByClassOrSubjectAsync(
        Guid schoolId,
        Guid? classId,
        Guid? subjectId);

    Task<(bool Success, object? Data, string? Error)>
        GetStudentByIdAsync(
            Guid schoolId,
            Guid studentId);

    Task<(bool Success, object? Data, string? Error)>
        UpdateStudentAsync(
            Guid schoolId,
            Guid studentId,
            UpdateStudentRequest request);

    Task<(bool Success, string? Error)>
        DeleteStudentAsync(
            Guid schoolId,
            Guid studentId);

              // ADMIN ATTENDANCE
    Task<(bool Success, object? Data, string? Error)>
        GetStudentAttendanceAsync(
            Guid schoolId,
            Guid studentId,
            string session,
            string term);

    // ADMIN ASSIGNMENTS
    Task<(bool Success, object? Data, string? Error)>
        GetStudentAssignmentsAsync(
            Guid schoolId,
            Guid studentId,
            string session,
            string term);
}