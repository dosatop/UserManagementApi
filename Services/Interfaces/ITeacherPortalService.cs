using UserManagementApi.DTOs.Results;

namespace UserManagementApi.Services.Interfaces;

public interface ITeacherPortalService
{
    Task<(bool Success, object? Data, string? Error)>
        GetProfileAsync(string userId);

    Task<(bool Success, object? Data, string? Error)>
        GetClassesAsync(string userId);

    Task<(bool Success, object? Data, string? Error)>
        GetSubjectsAsync(string userId);

    Task<(bool Success, object? Data, string? Error)>
   GetResultsAsync(
       Guid schoolId,
       Guid teacherId,
       GetTeacherResultsRequest request);

    Task<(bool Success, object? Data, string? Error)>
     CreateResultAsync(
         string userId,
         CreateResultRequest request);

    Task<(bool Success, object? Data, string? Error)>
        UpdateResultAsync(
            string userId,
            Guid resultId,
            CreateResultRequest request);

    Task<(bool Success, string? Error)>
        DeleteResultAsync(
            string userId,
            Guid resultId);
}