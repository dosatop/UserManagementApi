namespace UserManagementApi.Services.Interfaces;

public interface ITeacherPortalService
{
    Task<(bool Success, object? Data, string? Error)>
        GetProfileAsync(string userId);

    Task<(bool Success, object? Data, string? Error)>
        GetClassesAsync(string userId);

    Task<(bool Success, object? Data, string? Error)>
        GetSubjectsAsync(string userId);
}