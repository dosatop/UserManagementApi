namespace UserManagementApi.Services.Interfaces;

public interface IParentPortalService
{
    Task<(bool Success, object? Data, string? Error)>
        GetProfileAsync(string userId);

    Task<(bool Success, object? Data, string? Error)>
        GetChildrenAsync(string userId);

    Task<(bool Success, object? Data, string? Error)>
        GetChildAsync(
            string userId,
            Guid studentId);

             Task<(bool Success, object? Data, string? Error)>
        GetChildResultsAsync(
            string userId,
            Guid studentId,
            string session,
            string term);
}