using UserManagementApi.DTOs.Teachers;

namespace UserManagementApi.Services.Interfaces;

public interface ITeacherClassService
{
    Task<(bool Success, object? Data, string? Error)>
        AssignClassAsync(
            Guid schoolId,
            Guid teacherId,
            AssignTeacherClassRequest request);

    Task<IEnumerable<object>> GetTeacherClassesAsync(
        Guid schoolId,
        Guid teacherId);

    Task<(bool Success, string? Error)>
        RemoveClassAsync(
            Guid schoolId,
            Guid teacherId,
            Guid classId);
}