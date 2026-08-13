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
}