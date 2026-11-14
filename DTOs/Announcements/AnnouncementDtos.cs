using System.ComponentModel.DataAnnotations;
using UserManagementApi.Models.Announcements;

namespace UserManagementApi.DTOs.Announcements;

public class CreateAnnouncementRequest
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(5000)]
    public string Body { get; set; } = string.Empty;

    public AnnouncementAudience Audience { get; set; } = AnnouncementAudience.Everyone;

    public AnnouncementPriority Priority { get; set; } = AnnouncementPriority.Normal;

    /// Super admin only: which school. Null = every school.
    /// Admins and teachers: ignored (their own school is used).
    public Guid? SchoolId { get; set; }

    /// Optional: only this class. Required for teachers.
    public Guid? ClassId { get; set; }

    public bool IsPinned { get; set; }

    public DateTime? ExpiresAt { get; set; }

    /// Send a push notification to everyone it targets (default yes).
    public bool SendPush { get; set; } = true;
}

public class UpdateAnnouncementRequest
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(5000)]
    public string Body { get; set; } = string.Empty;

    public AnnouncementPriority Priority { get; set; } = AnnouncementPriority.Normal;

    public bool IsPinned { get; set; }

    public DateTime? ExpiresAt { get; set; }

    /// Push the edited version again (default no).
    public bool ResendPush { get; set; }
}

public class RegisterDeviceRequest
{
    [Required, MaxLength(512)]
    public string Token { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Platform { get; set; } = string.Empty;
}
