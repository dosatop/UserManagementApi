public class AdminAssignmentSubmissionDto
{
    public Guid Id { get; set; }

    public string? SubmissionText { get; set; }
    public string? AttachmentUrl { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public decimal? Score { get; set; }
    public string? Feedback { get; set; }

    public bool IsGraded { get; set; }
    public DateTime? GradedAt { get; set; }
}