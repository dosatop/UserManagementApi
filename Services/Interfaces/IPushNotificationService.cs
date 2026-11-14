namespace UserManagementApi.Services.Interfaces;

// ====================================================================
// PUSH NOTIFICATIONS (Firebase Cloud Messaging)
// ====================================================================

public interface IPushNotificationService
{
    /// Sends to every registered device of these users. Returns how many
    /// devices accepted it. Never throws — push failures must not break
    /// the request that triggered them.
    Task<int> SendToUsersAsync(
        IEnumerable<string> userIds,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);
}
