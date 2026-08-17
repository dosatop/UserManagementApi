using UserManagementApi.DTOs.Teachers;

namespace UserManagementApi.Services.Interfaces;

public interface ITeacherService
{
    Task<(bool Success, object? Data, string? Error)>
        CreateTeacherAsync(
            Guid schoolId,
            CreateTeacherRequest request);

    Task<IEnumerable<object>>
        GetTeachersAsync(
            Guid schoolId);

    Task<(bool Success, object? Data, string? Error)>
        GetTeacherByIdAsync(
            Guid schoolId,
            Guid teacherId);

    Task<(bool Success, object? Data, string? Error)>
        UpdateTeacherAsync(
            Guid schoolId,
            Guid teacherId,
            UpdateTeacherRequest request);

    Task<(bool Success, object? Data, string? Error)>
        DeleteTeacherAsync(
            Guid schoolId,
            Guid teacherId);
}