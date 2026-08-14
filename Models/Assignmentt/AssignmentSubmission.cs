namespace UserManagementApi.Models.Assignments;

public class AssignmentSubmission
{
    public Guid Id { get; set; }

    // ============================================================
    // ASSIGNMENT
    // ============================================================

    public Guid AssignmentId { get; set; }

    public Assignment Assignment { get; set; } = null!;

    // ============================================================
    // STUDENT
    // ============================================================

    public Guid StudentId { get; set; }

    public StudentProfile Student { get; set; } = null!;

    // ============================================================
    // SUBMISSION
    // ============================================================

    public string? SubmissionText { get; set; }

    public string? AttachmentUrl { get; set; }

    public DateTime? SubmittedAt { get; set; }

    // ============================================================
    // GRADING
    // ============================================================

    public decimal? Score { get; set; }

    public string? Feedback { get; set; }

    public bool IsGraded { get; set; } = false;

    public DateTime? GradedAt { get; set; }

    // ============================================================
    // AUDIT
    // ============================================================

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}