using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

// ====================================================================
// FIREBASE CLOUD MESSAGING
// ====================================================================
// NuGet:  dotnet add package FirebaseAdmin
//
// Firebase is initialised once in Program.cs from the service-account
// JSON (env var FIREBASE_CREDENTIALS_JSON). If it isn't configured this
// service logs a warning and sends nothing — the app keeps working.
//
// Dead tokens (app uninstalled, signed out) are deleted automatically.
//
// Register in Program.cs:
//   builder.Services.AddScoped<IPushNotificationService, FcmPushNotificationService>();

public class FcmPushNotificationService(
    ApplicationDbContext context,
    ILogger<FcmPushNotificationService> logger)
    : IPushNotificationService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<FcmPushNotificationService> _logger = logger;

    private const int BatchSize = 500;          // FCM multicast limit

    public async Task<int> SendToUsersAsync(
        IEnumerable<string> userIds,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        if (FirebaseApp.DefaultInstance == null)
        {
            _logger.LogWarning("Push skipped: Firebase is not configured (FIREBASE_CREDENTIALS_JSON).");
            return 0;
        }

        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return 0;

        var tokens = await _context.DeviceTokens.AsNoTracking()
            .Where(d => ids.Contains(d.UserId))
            .Select(d => d.Token)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (tokens.Count == 0) return 0;

        var payload = data?.ToDictionary(kv => kv.Key, kv => kv.Value)
                      ?? new Dictionary<string, string>();

        var delivered = 0;
        var deadTokens = new List<string>();

        foreach (var batch in tokens.Chunk(BatchSize))
        {
            var message = new MulticastMessage
            {
                Tokens = batch,
                Notification = new Notification { Title = title, Body = body },
                Data = payload,
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification
                    {
                        Sound = "default",
                                            }
                },
                Apns = new ApnsConfig
                {
                    Aps = new Aps { Sound = "default", ContentAvailable = true }
                }
            };

            try
            {
                var response = await FirebaseMessaging.DefaultInstance
                    .SendEachForMulticastAsync(message, cancellationToken);

                delivered += response.SuccessCount;

                for (var i = 0; i < response.Responses.Count; i++)
                {
                    var r = response.Responses[i];
                    if (r.IsSuccess) continue;

                    var code = r.Exception?.MessagingErrorCode;
                    if (code is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument)
                        deadTokens.Add(batch[i]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FCM batch send failed ({Count} tokens)", batch.Length);
            }
        }

        if (deadTokens.Count > 0)
        {
            await _context.DeviceTokens
                .Where(d => deadTokens.Contains(d.Token))
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation("Removed {Count} dead push tokens", deadTokens.Count);
        }

        return delivered;
    }
}
