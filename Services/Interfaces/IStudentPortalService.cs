namespace UserManagementApi.Services.Interfaces;

public interface IStudentPortalService
{
    Task<(bool Success, object? Data, string? Error)>
        GetProfileAsync(string userId);

    Task<(bool Success, object? Data, string? Error)>
        GetClassAsync(string userId);

    Task<(bool Success, object? Data, string? Error)>
        GetSubjectsAsync(string userId);
    Task<(bool Success, object? Data, string? Error)>
        GetResultsAsync(
            string userId,
            string session,
            string term);
}