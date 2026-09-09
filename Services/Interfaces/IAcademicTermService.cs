using UserManagementApi.DTOs.AcademicTerms;

public interface IAcademicTermService
{
    Task<(bool Success, object? Data, string? Error)>
        CreateAsync(
            Guid schoolId,
            CreateAcademicTermRequest request);

    Task<(bool Success, object? Data, string? Error)>
        GetAllAsync(
            Guid schoolId,
            Guid academicSessionId);

    Task<(bool Success, object? Data, string? Error)>
        GetByIdAsync(
            Guid schoolId,
            Guid termId);

    Task<(bool Success, object? Data, string? Error)>
        UpdateAsync(
            Guid schoolId,
            Guid termId,
            UpdateAcademicTermRequest request);

    Task<(bool Success, string? Error)>
        DeleteAsync(
            Guid schoolId,
            Guid termId);
}
