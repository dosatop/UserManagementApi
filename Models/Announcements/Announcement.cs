using UserManagementApi.Models.SchoolModels;   // ← School (same namespace your Subject.cs uses)

namespace UserManagementApi.Models.Announcements;

// ====================================================================
// ANNOUNCEMENTS
// ====================================================================
// Who receives an announcement is decided by three things:
//   SchoolId  null → every school (super admin only)
//   ClassId   set  → only that class (its students, their parents and,
//                    for Everyone/Teachers, the class's teachers)
//   Audience  which roles inside that scope

public enum AnnouncementAudience
{
    Everyone = 1,
    Teachers = 2,
    Students = 3,
    Parents = 4,
    StudentsAndParents = 5,
    Admins = 6
}

public enum AnnouncementPriority
{
    Normal = 1,
    Important = 2,
    Urgent = 3
}

public class Announcement
{
    public Guid Id { get; set; }

    // ---- scope -------------------------------------------------------
    public Guid? SchoolId { get; set; }
    public School? School { get; set; }

    public Guid? ClassId { get; set; }
    public Class? Class { get; set; }

    public AnnouncementAudience Audience { get; set; } = AnnouncementAudience.Everyone;

    // ---- content -----------------------------------------------------
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public AnnouncementPriority Priority { get; set; } = AnnouncementPriority.Normal;
    public bool IsPinned { get; set; }

    /// Hidden from feeds after this time (optional).
    public DateTime? ExpiresAt { get; set; }

    // ---- author (copied so the feed doesn't need joins) --------------
    public string CreatedById { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public string CreatedByRole { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// How many devices the push went to (for the author's info).
    public int PushSentCount { get; set; }

    public ICollection<AnnouncementRead> Reads { get; set; } = [];
}

public class AnnouncementRead
{
    public Guid Id { get; set; }

    public Guid AnnouncementId { get; set; }
    public Announcement Announcement { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;

    public DateTime ReadAt { get; set; } = DateTime.UtcNow;
}

// ====================================================================
// PUSH DEVICE TOKENS (one row per phone the user is signed in on)
// ====================================================================

public class DeviceToken
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    /// FCM registration token.
    public string Token { get; set; } = string.Empty;

    /// "android" | "ios" | "web"
    public string Platform { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
}
