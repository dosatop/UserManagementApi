using UserManagementApi.DTOs.Departments;

namespace UserManagementApi.Services.Interfaces;

public interface IDepartmentService
{
    Task<(bool Success, object? Data, string? Error)>
        CreateDepartmentAsync(
            Guid schoolId,
            CreateDepartmentRequest request);

    Task<IEnumerable<object>>
        GetDepartmentsAsync(Guid schoolId);

    Task<(bool Success, object? Data, string? Error)>
        GetDepartmentAsync(
            Guid schoolId,
            Guid departmentId);

    Task<(bool Success, object? Data, string? Error)>
        UpdateDepartmentAsync(
            Guid schoolId,
            Guid departmentId,
            CreateDepartmentRequest request);

    Task<(bool Success, string? Error)>
        DeleteDepartmentAsync(
            Guid schoolId,
            Guid departmentId);
}
