using UserManagementApi.Models.Assignments;
using UserManagementApi.Models.SchoolModels;

namespace UserManagementApi.Models;

public class Assignment
{
    public Guid Id { get; set; }

    // ============================================================
    // SCHOOL
    // ============================================================

    public Guid SchoolId { get; set; }

    public School School { get; set; } = null!;

    // ============================================================
    // TEACHER
    // ============================================================

    public Guid TeacherId { get; set; }

    public Teacher Teacher { get; set; } = null!;

    // ============================================================
    // CLASS
    // ============================================================

    public Guid ClassId { get; set; }

    public Class Class { get; set; } = null!;

    // ============================================================
    // SUBJECT
    // ============================================================

    public Guid SubjectId { get; set; }

    public Subject Subject { get; set; } = null!;

    // ============================================================
    // ASSIGNMENT DETAILS
    // ============================================================

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Optional attachment
    public string? AttachmentUrl { get; set; }

    // ============================================================
    // ACADEMIC PERIOD
    // ============================================================

    public string Session { get; set; } = string.Empty;

    public string Term { get; set; } = string.Empty;

    // ============================================================
    // DATES
    // ============================================================

    public DateTime AssignedAt { get; set; }

    public DateTime? DueDate { get; set; }

    // ============================================================
    // STATUS
    // ============================================================

    public bool IsPublished { get; set; } = true;

    // ============================================================
    // AUDIT
    // ============================================================

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // ============================================================
    // SUBMISSIONS
    // ============================================================

    public ICollection<AssignmentSubmission> Submissions { get; set; } = [];
}