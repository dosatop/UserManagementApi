

using UserManagementApi.DTOs.Teachers;

namespace UserManagementApi.Services.Interfaces;

public interface ITeacherService
{
    Task<(bool Success, object? Data, string? Error)>
        CreateTeacherAsync(
            Guid schoolId,
            CreateTeacherRequest request);

    Task<IEnumerable<object>> GetTeachersAsync(
        Guid schoolId);
}