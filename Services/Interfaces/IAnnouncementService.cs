using UserManagementApi.DTOs.Announcements;

namespace UserManagementApi.Services.Interfaces;

// ====================================================================
// ANNOUNCEMENT SERVICE
// ====================================================================
// userId = the signed-in user (from the token). The service looks up
// their role and school itself, so controllers stay thin.

public record AnnouncementResult(
    bool Success,
    object? Data = null,
    string? Error = null,
    int Status = 200)
{
    public static AnnouncementResult Ok(object data) => new(true, data);
    public static AnnouncementResult Fail(string error) => new(false, null, error, 400);
    public static AnnouncementResult Missing(string error) => new(false, null, error, 404);
    public static AnnouncementResult Forbidden(string error) => new(false, null, error, 403);
}

public interface IAnnouncementService
{
    // ---- reading (every role) ---------------------------------------
    Task<AnnouncementResult> GetFeedAsync(
        string userId, int page, int pageSize, bool unreadOnly, bool mine);

    Task<AnnouncementResult> GetUnreadCountAsync(string userId);

    Task<AnnouncementResult> GetAnnouncementAsync(string userId, Guid id);

    Task<AnnouncementResult> MarkReadAsync(string userId, Guid id);

    Task<AnnouncementResult> MarkAllReadAsync(string userId);

    // ---- writing (SuperAdmin, Admin, Teacher) -----------------------
    /// What the user may target: schools, classes and audiences.
    Task<AnnouncementResult> GetComposeOptionsAsync(string userId);

    Task<AnnouncementResult> CreateAsync(string userId, CreateAnnouncementRequest request);

    Task<AnnouncementResult> UpdateAsync(string userId, Guid id, UpdateAnnouncementRequest request);

    Task<AnnouncementResult> DeleteAsync(string userId, Guid id);

    // ---- push devices -----------------------------------------------
    Task<AnnouncementResult> RegisterDeviceAsync(string userId, RegisterDeviceRequest request);

    Task<AnnouncementResult> UnregisterDeviceAsync(string userId, string token);
}
