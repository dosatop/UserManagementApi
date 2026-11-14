using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Announcements;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.Models.Announcements;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

// ====================================================================
// ANNOUNCEMENT SERVICE
// ====================================================================
// Who can post:
//   SuperAdmin → one school or every school, any audience, any class
//   Admin      → their own school, any audience except Admins
//   Teacher    → only classes they teach (class or subject teacher),
//                to that class's students and/or parents
//
// Who sees it (feed + push):
//   SchoolId null = every school;  ClassId set = only that class
//   Audience picks the roles inside that scope.
//   Admins and super admins also see everything in their scope so they
//   can manage it.
//
// Register in Program.cs:
//   builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();

public class AnnouncementService(
    ApplicationDbContext context,
    IPushNotificationService push,
    ILogger<AnnouncementService> logger)
    : IAnnouncementService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IPushNotificationService _push = push;
    private readonly ILogger<AnnouncementService> _logger = logger;

    private const int MaxPageSize = 50;

    // ================================================================
    // FEED
    // ================================================================

    public async Task<AnnouncementResult> GetFeedAsync(
        string userId,
        int page,
        int pageSize,
        bool unreadOnly,
        bool mine)
    {
        var caller = await GetCallerAsync(userId);
        if (caller == null) return AnnouncementResult.Forbidden("Account not found.");

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = await VisibleQueryAsync(caller);

        if (mine)
            query = query.Where(a => a.CreatedById == userId);

        if (unreadOnly)
            query = query.Where(a =>
                a.CreatedById != userId &&
                !a.Reads.Any(r => r.UserId == userId));

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(Project(userId))
            .ToListAsync();

        var unreadCount = await CountUnreadAsync(caller);

        return AnnouncementResult.Ok(new
        {
            page,
            pageSize,
            total,
            hasMore = page * pageSize < total,
            unreadCount,
            items = items.Select(i => WithPermissions(i, caller))
        });
    }

    public async Task<AnnouncementResult> GetUnreadCountAsync(string userId)
    {
        var caller = await GetCallerAsync(userId);
        if (caller == null) return AnnouncementResult.Forbidden("Account not found.");

        return AnnouncementResult.Ok(new { unreadCount = await CountUnreadAsync(caller) });
    }

    public async Task<AnnouncementResult> GetAnnouncementAsync(string userId, Guid id)
    {
        var caller = await GetCallerAsync(userId);
        if (caller == null) return AnnouncementResult.Forbidden("Account not found.");

        var item = await (await VisibleQueryAsync(caller))
            .Where(a => a.Id == id)
            .Select(Project(userId))
            .FirstOrDefaultAsync();

        if (item == null) return AnnouncementResult.Missing("Announcement not found.");

        return AnnouncementResult.Ok(WithPermissions(item, caller));
    }

    // ================================================================
    // READ RECEIPTS
    // ================================================================

    public async Task<AnnouncementResult> MarkReadAsync(string userId, Guid id)
    {
        var caller = await GetCallerAsync(userId);
        if (caller == null) return AnnouncementResult.Forbidden("Account not found.");

        var visible = await (await VisibleQueryAsync(caller)).AnyAsync(a => a.Id == id);
        if (!visible) return AnnouncementResult.Missing("Announcement not found.");

        var already = await _context.AnnouncementReads
            .AnyAsync(r => r.AnnouncementId == id && r.UserId == userId);

        if (!already)
        {
            _context.AnnouncementReads.Add(new AnnouncementRead
            {
                Id = Guid.NewGuid(),
                AnnouncementId = id,
                UserId = userId
            });

            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateException) { /* read twice at the same time — fine */ }
        }

        return AnnouncementResult.Ok(new
        {
            message = "Marked as read.",
            unreadCount = await CountUnreadAsync(caller)
        });
    }

    public async Task<AnnouncementResult> MarkAllReadAsync(string userId)
    {
        var caller = await GetCallerAsync(userId);
        if (caller == null) return AnnouncementResult.Forbidden("Account not found.");

        var unreadIds = await (await VisibleQueryAsync(caller))
            .Where(a => a.CreatedById != userId && !a.Reads.Any(r => r.UserId == userId))
            .Select(a => a.Id)
            .ToListAsync();

        _context.AnnouncementReads.AddRange(unreadIds.Select(id => new AnnouncementRead
        {
            Id = Guid.NewGuid(),
            AnnouncementId = id,
            UserId = userId
        }));

        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateException) { /* some were marked meanwhile — fine */ }

        return AnnouncementResult.Ok(new { message = "All caught up.", marked = unreadIds.Count, unreadCount = 0 });
    }

    // ================================================================
    // COMPOSE OPTIONS — what this user may target
    // ================================================================

    public async Task<AnnouncementResult> GetComposeOptionsAsync(string userId)
    {
        var caller = await GetCallerAsync(userId);
        if (caller == null) return AnnouncementResult.Forbidden("Account not found.");

        if (!CanPost(caller.Role))
            return AnnouncementResult.Forbidden("Only admins and teachers can post announcements.");

        var audiences = AllowedAudiences(caller.Role)
            .Select(a => new { value = (int)a, name = a.ToString(), label = AudienceLabel(a) })
            .ToList();

        // ---- schools (super admin only) --------------------------------
        var isSuperAdmin = caller.Role == Roles.SuperAdmin;

        var schools = await _context.Schools.AsNoTracking()
            .Where(_ => isSuperAdmin)   // empty list for everyone else
            .OrderBy(s => s.Name)
            .Select(s => new { schoolId = s.Id, schoolName = s.Name })
            .ToListAsync();

        // ---- classes ----------------------------------------------------
        var classQuery = _context.Classes.AsNoTracking();

        if (caller.Role == Roles.Teacher)
        {
            var mine = await TeacherClassIdsAsync(caller);
            classQuery = classQuery.Where(c => mine.Contains(c.Id));
        }
        else if (caller.Role == Roles.Admin)
        {
            classQuery = classQuery.Where(c => c.SchoolId == caller.SchoolId);
        }

        var classes = await classQuery
            .OrderBy(c => c.Name)
            .Select(c => new { classId = c.Id, className = c.Name, schoolId = c.SchoolId })
            .ToListAsync();

        return AnnouncementResult.Ok(new
        {
            role = caller.Role,
            schoolId = caller.SchoolId,
            requiresClass = caller.Role == Roles.Teacher,
            canTargetAllSchools = caller.Role == Roles.SuperAdmin,
            audiences,
            schools,
            classes
        });
    }

    // ================================================================
    // CREATE (+ push)
    // ================================================================

    public async Task<AnnouncementResult> CreateAsync(
        string userId,
        CreateAnnouncementRequest request)
    {
        var caller = await GetCallerAsync(userId);
        if (caller == null) return AnnouncementResult.Forbidden("Account not found.");

        if (!CanPost(caller.Role))
            return AnnouncementResult.Forbidden("Only admins and teachers can post announcements.");

        var title = request.Title.Trim();
        var body = request.Body.Trim();

        if (title.Length == 0) return AnnouncementResult.Fail("Title is required.");
        if (body.Length == 0) return AnnouncementResult.Fail("Message is required.");

        if (!Enum.IsDefined(request.Audience))
            return AnnouncementResult.Fail("Choose who the announcement is for.");

        if (!AllowedAudiences(caller.Role).Contains(request.Audience))
            return AnnouncementResult.Forbidden("You can't send announcements to that group.");

        if (request.ExpiresAt.HasValue && request.ExpiresAt.Value <= DateTime.UtcNow)
            return AnnouncementResult.Fail("The expiry date must be in the future.");

        // ---- school -------------------------------------------------------
        Guid? schoolId;

        if (caller.Role == Roles.SuperAdmin)
        {
            schoolId = request.SchoolId;

            if (schoolId.HasValue &&
                !await _context.Schools.AnyAsync(s => s.Id == schoolId.Value))
                return AnnouncementResult.Fail("School not found.");
        }
        else
        {
            if (caller.SchoolId == null)
                return AnnouncementResult.Forbidden("Your account isn't linked to a school.");

            schoolId = caller.SchoolId;
        }

        // ---- class --------------------------------------------------------
        if (request.ClassId.HasValue)
        {
            if (schoolId == null)
                return AnnouncementResult.Fail("Pick a school before picking a class.");

            var classOk = await _context.Classes.AnyAsync(c =>
                c.Id == request.ClassId.Value && c.SchoolId == schoolId.Value);

            if (!classOk)
                return AnnouncementResult.Fail("That class doesn't belong to this school.");

            if (caller.Role == Roles.Teacher)
            {
                var mine = await TeacherClassIdsAsync(caller);
                if (!mine.Contains(request.ClassId.Value))
                    return AnnouncementResult.Forbidden("You can only post to classes you teach.");
            }
        }
        else if (caller.Role == Roles.Teacher)
        {
            return AnnouncementResult.Fail("Choose the class this announcement is for.");
        }

        // ---- save ---------------------------------------------------------
        var announcement = new Announcement
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            ClassId = request.ClassId,
            Audience = request.Audience,
            Priority = Enum.IsDefined(request.Priority) ? request.Priority : AnnouncementPriority.Normal,
            Title = title,
            Body = body,
            IsPinned = request.IsPinned,
            ExpiresAt = request.ExpiresAt,
            CreatedById = caller.UserId,
            CreatedByName = caller.FullName,
            CreatedByRole = caller.Role,
            CreatedAt = DateTime.UtcNow
        };

        _context.Announcements.Add(announcement);
        await _context.SaveChangesAsync();

        // ---- push ---------------------------------------------------------
        var recipients = 0;
        var devices = 0;

        if (request.SendPush)
            (recipients, devices) = await PushAsync(announcement);

        var item = await _context.Announcements.AsNoTracking()
            .Where(a => a.Id == announcement.Id)
            .Select(Project(userId))
            .FirstAsync();

        return AnnouncementResult.Ok(new
        {
            message = request.SendPush
                ? $"Announcement posted and sent to {recipients} {(recipients == 1 ? "person" : "people")}."
                : "Announcement posted.",
            recipients,
            devices,
            announcement = WithPermissions(item, caller)
        });
    }

    // ================================================================
    // UPDATE
    // ================================================================

    public async Task<AnnouncementResult> UpdateAsync(
        string userId,
        Guid id,
        UpdateAnnouncementRequest request)
    {
        var caller = await GetCallerAsync(userId);
        if (caller == null) return AnnouncementResult.Forbidden("Account not found.");

        var announcement = await _context.Announcements.FirstOrDefaultAsync(a => a.Id == id);
        if (announcement == null) return AnnouncementResult.Missing("Announcement not found.");

        if (!CanManage(caller, announcement.CreatedById, announcement.SchoolId))
            return AnnouncementResult.Forbidden("You can't edit this announcement.");

        var title = request.Title.Trim();
        var body = request.Body.Trim();

        if (title.Length == 0) return AnnouncementResult.Fail("Title is required.");
        if (body.Length == 0) return AnnouncementResult.Fail("Message is required.");

        announcement.Title = title;
        announcement.Body = body;
        announcement.Priority = Enum.IsDefined(request.Priority) ? request.Priority : announcement.Priority;
        announcement.IsPinned = request.IsPinned;
        announcement.ExpiresAt = request.ExpiresAt;
        announcement.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        if (request.ResendPush)
            await PushAsync(announcement, updated: true);

        var item = await _context.Announcements.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(Project(userId))
            .FirstAsync();

        return AnnouncementResult.Ok(new
        {
            message = "Announcement updated.",
            announcement = WithPermissions(item, caller)
        });
    }

    // ================================================================
    // DELETE
    // ================================================================

    public async Task<AnnouncementResult> DeleteAsync(string userId, Guid id)
    {
        var caller = await GetCallerAsync(userId);
        if (caller == null) return AnnouncementResult.Forbidden("Account not found.");

        var announcement = await _context.Announcements.FirstOrDefaultAsync(a => a.Id == id);
        if (announcement == null) return AnnouncementResult.Missing("Announcement not found.");

        if (!CanManage(caller, announcement.CreatedById, announcement.SchoolId))
            return AnnouncementResult.Forbidden("You can't delete this announcement.");

        _context.Announcements.Remove(announcement);   // reads cascade
        await _context.SaveChangesAsync();

        return AnnouncementResult.Ok(new { message = "Announcement deleted." });
    }

    // ================================================================
    // DEVICES
    // ================================================================

    public async Task<AnnouncementResult> RegisterDeviceAsync(
        string userId,
        RegisterDeviceRequest request)
    {
        var token = request.Token.Trim();
        if (token.Length == 0) return AnnouncementResult.Fail("Device token is required.");

        var platform = (request.Platform ?? string.Empty).Trim().ToLowerInvariant();

        // A phone has one token. If someone else signed in on this phone
        // before, the token moves to the current user.
        var existing = await _context.DeviceTokens.FirstOrDefaultAsync(d => d.Token == token);

        if (existing == null)
        {
            _context.DeviceTokens.Add(new DeviceToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = token,
                Platform = platform
            });
        }
        else
        {
            existing.UserId = userId;
            existing.Platform = platform.Length > 0 ? platform : existing.Platform;
            existing.LastSeenAt = DateTime.UtcNow;
        }

        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateException) { /* registered twice at once — fine */ }

        return AnnouncementResult.Ok(new { message = "Device registered." });
    }

    public async Task<AnnouncementResult> UnregisterDeviceAsync(string userId, string token)
    {
        await _context.DeviceTokens
            .Where(d => d.Token == token && d.UserId == userId)
            .ExecuteDeleteAsync();

        return AnnouncementResult.Ok(new { message = "Device removed." });
    }

    // ================================================================
    // PUSH
    // ================================================================

    private async Task<(int Recipients, int Devices)> PushAsync(
        Announcement a,
        bool updated = false)
    {
        try
        {
            var userIds = await RecipientUserIdsAsync(a);
            if (userIds.Count == 0) return (0, 0);

            var title = a.Priority == AnnouncementPriority.Urgent
                ? $"🔴 {a.Title}"
                : updated ? $"Updated: {a.Title}" : a.Title;

            var body = a.Body.Length > 180 ? a.Body[..177] + "…" : a.Body;

            var devices = await _push.SendToUsersAsync(
                userIds,
                title,
                body,
                new Dictionary<string, string>
                {
                    ["type"] = "announcement",
                    ["announcementId"] = a.Id.ToString(),
                    ["priority"] = ((int)a.Priority).ToString()
                });

            a.PushSentCount += devices;
            await _context.SaveChangesAsync();

            return (userIds.Count, devices);
        }
        catch (Exception ex)
        {
            // The announcement is saved either way.
            _logger.LogError(ex, "Push for announcement {Id} failed", a.Id);
            return (0, 0);
        }
    }

    /// Everyone the announcement is meant for (author excluded).
    private async Task<List<string>> RecipientUserIdsAsync(Announcement a)
    {
        var roles = RolesFor(a.Audience);
        var ids = new HashSet<string>();

        if (a.ClassId.HasValue)
        {
            var classId = a.ClassId.Value;

            var studentIds = _context.StudentProfiles
                .Where(s => s.ClassId == classId)
                .Select(s => s.Id);

            if (roles.Contains(Roles.Student))
                ids.UnionWith(await _context.StudentProfiles
                    .Where(s => s.ClassId == classId)
                    .Select(s => s.UserId)
                    .ToListAsync());

            if (roles.Contains(Roles.Parent))
                ids.UnionWith(await ParentUserIdsOfStudents(studentIds).ToListAsync());

            if (roles.Contains(Roles.Teacher))
                ids.UnionWith(await TeacherUserIdsOfClassAsync(classId));

            if (roles.Contains(Roles.Admin) && a.SchoolId.HasValue)
                ids.UnionWith(await UsersInRolesQuery([Roles.Admin], a.SchoolId).ToListAsync());
        }
        else
        {
            ids.UnionWith(await UsersInRolesQuery(roles, a.SchoolId).ToListAsync());

            // Parents may not have a SchoolId on their account — reach
            // them through their children too.
            if (roles.Contains(Roles.Parent))
            {
                var studentIds = _context.StudentProfiles
                    .Where(s => a.SchoolId == null || s.SchoolId == a.SchoolId)
                    .Select(s => s.Id);

                ids.UnionWith(await ParentUserIdsOfStudents(studentIds).ToListAsync());
            }
        }

        ids.Remove(a.CreatedById);
        return ids.ToList();
    }

    // ================================================================
    // VISIBILITY
    // ================================================================

    private async Task<IQueryable<Announcement>> VisibleQueryAsync(Caller c)
    {
        var now = DateTime.UtcNow;
        var q = _context.Announcements.AsNoTracking();

        switch (c.Role)
        {
            case var r when r == Roles.SuperAdmin:
                return q;

            case var r when r == Roles.Admin:
                return q.Where(a => a.SchoolId == null || a.SchoolId == c.SchoolId);
        }

        // Everyone else: hide expired ones (except their own posts).
        q = q.Where(a => a.CreatedById == c.UserId || a.ExpiresAt == null || a.ExpiresAt > now);

        if (c.Role == Roles.Teacher)
        {
            var classIds = await TeacherClassIdsAsync(c);

            return q.Where(a =>
                a.CreatedById == c.UserId ||
                ((a.SchoolId == null || a.SchoolId == c.SchoolId) &&
                 (a.Audience == AnnouncementAudience.Everyone ||
                  a.Audience == AnnouncementAudience.Teachers) &&
                 (a.ClassId == null || classIds.Contains(a.ClassId.Value))));
        }

        if (c.Role == Roles.Student)
        {
            var student = await _context.StudentProfiles.AsNoTracking()
                .Where(s => s.UserId == c.UserId)
                .Select(s => new { s.ClassId, s.SchoolId })
                .FirstOrDefaultAsync();

            if (student == null) return q.Where(_ => false);

            return q.Where(a =>
                (a.SchoolId == null || a.SchoolId == student.SchoolId) &&
                (a.Audience == AnnouncementAudience.Everyone ||
                 a.Audience == AnnouncementAudience.Students ||
                 a.Audience == AnnouncementAudience.StudentsAndParents) &&
                (a.ClassId == null || a.ClassId == student.ClassId));
        }

        if (c.Role == Roles.Parent)
        {
            var children = await ChildrenOfParentAsync(c.UserId);
            var classIds = children.Select(x => x.ClassId).Distinct().ToList();
            var schoolIds = children.Select(x => x.SchoolId).Distinct().ToList();

            if (c.SchoolId.HasValue) schoolIds.Add(c.SchoolId.Value);

            return q.Where(a =>
                (a.SchoolId == null || schoolIds.Contains(a.SchoolId.Value)) &&
                (a.Audience == AnnouncementAudience.Everyone ||
                 a.Audience == AnnouncementAudience.Parents ||
                 a.Audience == AnnouncementAudience.StudentsAndParents) &&
                (a.ClassId == null || classIds.Contains(a.ClassId.Value)));
        }

        return q.Where(_ => false);
    }

    private async Task<int> CountUnreadAsync(Caller c) =>
        await (await VisibleQueryAsync(c))
            .CountAsync(a => a.CreatedById != c.UserId && !a.Reads.Any(r => r.UserId == c.UserId));

    // ================================================================
    // PROJECTION
    // ================================================================

    private record FeedItem(
        Guid Id,
        string Title,
        string Body,
        int Audience,
        int Priority,
        bool IsPinned,
        Guid? SchoolId,
        string? SchoolName,
        Guid? ClassId,
        string? ClassName,
        string CreatedById,
        string CreatedByName,
        string CreatedByRole,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        DateTime? ExpiresAt,
        bool IsRead,
        int ReadCount,
        int PushSentCount);

    private static System.Linq.Expressions.Expression<Func<Announcement, FeedItem>> Project(string userId) =>
        a => new FeedItem(
            a.Id,
            a.Title,
            a.Body,
            (int)a.Audience,
            (int)a.Priority,
            a.IsPinned,
            a.SchoolId,
            a.School != null ? a.School.Name : null,
            a.ClassId,
            a.Class != null ? a.Class.Name : null,
            a.CreatedById,
            a.CreatedByName,
            a.CreatedByRole,
            a.CreatedAt,
            a.UpdatedAt,
            a.ExpiresAt,
            a.CreatedById == userId || a.Reads.Any(r => r.UserId == userId),
            a.Reads.Count(),
            a.PushSentCount);

    private object WithPermissions(FeedItem i, Caller c)
    {
        var canManage = CanManage(c, i.CreatedById, i.SchoolId);

        return new
        {
            id = i.Id,
            title = i.Title,
            body = i.Body,
            audience = i.Audience,
            audienceLabel = AudienceLabel((AnnouncementAudience)i.Audience),
            priority = i.Priority,
            isPinned = i.IsPinned,
            schoolId = i.SchoolId,
            schoolName = i.SchoolId == null ? "All schools" : i.SchoolName,
            classId = i.ClassId,
            className = i.ClassName,
            createdByName = i.CreatedByName,
            createdByRole = i.CreatedByRole,
            isMine = i.CreatedById == c.UserId,
            createdAt = DateTime.SpecifyKind(i.CreatedAt, DateTimeKind.Utc),
            updatedAt = i.UpdatedAt.HasValue ? DateTime.SpecifyKind(i.UpdatedAt.Value, DateTimeKind.Utc) : (DateTime?)null,
            expiresAt = i.ExpiresAt.HasValue ? DateTime.SpecifyKind(i.ExpiresAt.Value, DateTimeKind.Utc) : (DateTime?)null,
            isRead = i.IsRead,
            canManage,
            // Only people who can manage it see reach numbers.
            readCount = canManage ? i.ReadCount : (int?)null,
            pushSentCount = canManage ? i.PushSentCount : (int?)null
        };
    }

    // ================================================================
    // RULES
    // ================================================================

    private static bool CanPost(string role) =>
        role == Roles.SuperAdmin || role == Roles.Admin || role == Roles.Teacher;

    private static bool CanManage(Caller c, string createdById, Guid? schoolId) =>
        createdById == c.UserId ||
        c.Role == Roles.SuperAdmin ||
        (c.Role == Roles.Admin && schoolId != null && schoolId == c.SchoolId);

    private static AnnouncementAudience[] AllowedAudiences(string role)
    {
        if (role == Roles.SuperAdmin) return Enum.GetValues<AnnouncementAudience>();

        if (role == Roles.Admin)
            return
            [
                AnnouncementAudience.Everyone,
                AnnouncementAudience.Teachers,
                AnnouncementAudience.Students,
                AnnouncementAudience.Parents,
                AnnouncementAudience.StudentsAndParents
            ];

        if (role == Roles.Teacher)
            return
            [
                AnnouncementAudience.StudentsAndParents,
                AnnouncementAudience.Students,
                AnnouncementAudience.Parents
            ];

        return [];
    }

    private static string[] RolesFor(AnnouncementAudience audience) => audience switch
    {
        AnnouncementAudience.Teachers => [Roles.Teacher],
        AnnouncementAudience.Students => [Roles.Student],
        AnnouncementAudience.Parents => [Roles.Parent],
        AnnouncementAudience.StudentsAndParents => [Roles.Student, Roles.Parent],
        AnnouncementAudience.Admins => [Roles.Admin],
        _ => [Roles.Admin, Roles.Teacher, Roles.Student, Roles.Parent]
    };

    private static string AudienceLabel(AnnouncementAudience audience) => audience switch
    {
        AnnouncementAudience.Teachers => "Teachers",
        AnnouncementAudience.Students => "Students",
        AnnouncementAudience.Parents => "Parents",
        AnnouncementAudience.StudentsAndParents => "Students & parents",
        AnnouncementAudience.Admins => "School admins",
        _ => "Everyone"
    };

    // ================================================================
    // LOOKUPS
    // ================================================================

    private record Caller(string UserId, string Role, Guid? SchoolId, string FullName);

    private async Task<Caller?> GetCallerAsync(string userId)
    {
        var user = await _context.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.FullName, u.SchoolId })
            .FirstOrDefaultAsync();

        if (user == null) return null;

        var roles = await (
            from ur in _context.UserRoles
            join r in _context.Roles on ur.RoleId equals r.Id
            where ur.UserId == userId
            select r.Name).ToListAsync();

        // If someone has more than one role, the most powerful wins.
        string[] order = [Roles.SuperAdmin, Roles.Admin, Roles.Teacher, Roles.Parent, Roles.Student];
        var role = order.FirstOrDefault(roles.Contains) ?? roles.FirstOrDefault() ?? string.Empty;

        return new Caller(user.Id, role, user.SchoolId, user.FullName ?? "Someone");
    }

    private IQueryable<string> UsersInRolesQuery(string[] roleNames, Guid? schoolId) =>
        from u in _context.Users
        join ur in _context.UserRoles on u.Id equals ur.UserId
        join r in _context.Roles on ur.RoleId equals r.Id
        where roleNames.Contains(r.Name!) && (schoolId == null || u.SchoolId == schoolId)
        select u.Id;

    private async Task<List<Guid>> TeacherClassIdsAsync(Caller c)
    {
        var teacherId = await _context.Teachers.AsNoTracking()
            .Where(t => t.UserId == c.UserId)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync();

        if (teacherId == null) return [];

        var a = await _context.TeacherClasses.Where(x => x.TeacherId == teacherId).Select(x => x.ClassId).ToListAsync();
        var b = await _context.ClassTeachers.Where(x => x.TeacherId == teacherId).Select(x => x.ClassId).ToListAsync();
        var s = await _context.TeacherSubjects.Where(x => x.TeacherId == teacherId).Select(x => x.ClassId).ToListAsync();

        return a.Concat(b).Concat(s).Distinct().ToList();
    }

    private async Task<List<string>> TeacherUserIdsOfClassAsync(Guid classId)
    {
        var teacherIds = await _context.TeacherClasses.Where(x => x.ClassId == classId).Select(x => x.TeacherId)
            .Union(_context.ClassTeachers.Where(x => x.ClassId == classId).Select(x => x.TeacherId))
            .Union(_context.TeacherSubjects.Where(x => x.ClassId == classId).Select(x => x.TeacherId))
            .ToListAsync();

        return await _context.Teachers
            .Where(t => teacherIds.Contains(t.Id))
            .Select(t => t.UserId)
            .ToListAsync();
    }

    // ----------------------------------------------------------------
    // PARENT LINKS
    // ----------------------------------------------------------------
    // Parents ←→ ParentStudents (ParentId, StudentId) ←→ StudentProfiles.

    private IQueryable<string> ParentUserIdsOfStudents(IQueryable<Guid> studentIds) =>
        from link in _context.ParentStudents
        join p in _context.Parents on link.ParentId equals p.ParentId
        where studentIds.Contains(link.StudentId)
        select p.UserId;

    private record Child(Guid ClassId, Guid SchoolId);

    private async Task<List<Child>> ChildrenOfParentAsync(string parentUserId) =>
        await (
            from link in _context.ParentStudents
            join p in _context.Parents on link.ParentId equals p.ParentId
            join s in _context.StudentProfiles on link.StudentId equals s.Id
            where p.UserId == parentUserId
            select new Child(s.ClassId, s.SchoolId)).ToListAsync();
}
