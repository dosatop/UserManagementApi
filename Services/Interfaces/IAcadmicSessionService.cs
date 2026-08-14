using UserManagementApi.DTOs.AcademicSessions;

namespace UserManagementApi.Services.Interfaces;

public interface IAcademicSessionService
{
    Task<(bool Success, object? Data, string? Error)>
        CreateAsync(
            Guid schoolId,
            CreateAcademicSessionRequest request);

    Task<(bool Success, object? Data, string? Error)>
        GetAllAsync(
            Guid schoolId);

    Task<(bool Success, object? Data, string? Error)>
        GetCurrentAsync(
            Guid schoolId);

    Task<(bool Success, object? Data, string? Error)>
        GetByIdAsync(
            Guid schoolId,
            Guid sessionId);

    Task<(bool Success, object? Data, string? Error)>
        UpdateAsync(
            Guid schoolId,
            Guid sessionId,
            UpdateAcademicSessionRequest request);

    Task<(bool Success, object? Data, string? Error)>
        ActivateAsync(
            Guid schoolId,
            Guid sessionId);

    Task<(bool Success, string? Error)>
        DeleteAsync(
            Guid schoolId,
            Guid sessionId);
}