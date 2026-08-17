using UserManagementApi.DTOs.Classes;

namespace UserManagementApi.Services.Interfaces;

public interface IClassService
{
    Task<(bool Success, object? Data, string? Error)>
        CreateClassAsync(
            Guid schoolId,
            CreateClassRequest request);

    Task<IEnumerable<object>>
        GetClassesAsync(Guid schoolId);

    Task<(bool Success, object? Data, string? Error)>
        GetClassAsync(
            Guid schoolId,
            Guid classId);

    Task<(bool Success, object? Data, string? Error)>
        UpdateClassAsync(
            Guid schoolId,
            Guid classId,
            CreateClassRequest request);

    Task<(bool Success, string? Error)>
        DeleteClassAsync(
            Guid schoolId,
            Guid classId);

            Task<(bool Success, object? Data, string? Error)>
    GetClassAssignmentsAsync(
        Guid schoolId,
        Guid classId,
        string session,
        string term);

        Task<(bool Success, object? Data, string? Error)>
    GetAssignmentCountAsync(
        Guid schoolId,
        Guid classId,
        string session,
        string term);
        
        Task<(bool Success, object? Data, string? Error)>
    GetSchoolAssignmentCountAsync(
        Guid schoolId,
        string session,
        string term);
        Task<(bool Success, object? Data, string? Error)>
    GetClassStudentsAsync(
        Guid schoolId,
        Guid classId,
        Guid? subjectId);
}