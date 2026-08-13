using UserManagementApi.DTOs.Subjects;

namespace UserManagementApi.Services.Interfaces;

public interface ISubjectService
{
    Task<(bool Success, object? Data, string? Error)>
        CreateSubjectAsync(
            Guid schoolId,
            CreateSubjectRequest request);

    Task<IEnumerable<object>>
        GetSubjectsAsync(Guid schoolId);

    Task<(bool Success, object? Data, string? Error)>
        GetSubjectAsync(
            Guid schoolId,
            Guid subjectId);

    Task<(bool Success, object? Data, string? Error)>
        UpdateSubjectAsync(
            Guid schoolId,
            Guid subjectId,
            CreateSubjectRequest request);

    Task<(bool Success, string? Error)>
        DeleteSubjectAsync(
            Guid schoolId,
            Guid subjectId);
}