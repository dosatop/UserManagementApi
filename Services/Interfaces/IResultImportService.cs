using UserManagementApi.DTOs.Results;

namespace UserManagementApi.Services.Interfaces;

public interface IResultImportService
{
    Task<(
        bool Success,
        ResultImportPreviewResponse? Data,
        string? Error
    )> PreviewAsync(
        Guid schoolId,
        ImportResultsRequest request);

    Task<(
        bool Success,
        object? Data,
        string? Error
    )> ConfirmImportAsync(
        Guid schoolId,
        ConfirmResultImportRequest request);

    // Get results
    Task<(
        bool Success,
        object? Data,
        string? Error
    )> GetAsync(
        Guid schoolId,
        GetResultsRequest request);

    Task<(
        bool Success,
        object? Data,
        string? Error
    )> CreateAsync(
        Guid schoolId,
        CreateResultRequest request);

    Task<(
        bool Success,
        object? Data,
        string? Error
    )> UpdateAsync(
        Guid schoolId,
        Guid resultId,
        UpdateResultRequest request);

    Task<(
        bool Success,
        string? Error
    )> DeleteAsync(
        Guid schoolId,
        Guid resultId);
}