using UserManagementApi.DTOs.Parents;

namespace UserManagementApi.Services.Interfaces;

public interface IParentService
{
    Task<(bool Success, object? Data, string? Error)>
        CreateParentAsync(
            Guid schoolId,
            CreateParentRequest request);

    Task<IEnumerable<object>>
        GetParentsAsync(Guid schoolId);

    Task<(bool Success, object? Data, string? Error)>
        AssignStudentAsync(
            Guid schoolId,
            Guid parentId,
            Guid studentId);

    Task<(bool Success, string? Error)>
        RemoveStudentAsync(
            Guid schoolId,
            Guid parentId,
            Guid studentId);
}