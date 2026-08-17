public class AdminStudentAssignmentDto
{
    public Guid AssignmentId { get; set; }

    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? AttachmentUrl { get; set; }

    public DateTime AssignedAt { get; set; }
    public DateTime? DueDate { get; set; }

    public string? Session { get; set; }
    public string? Term { get; set; }

    public Guid ClassId { get; set; }
    public string? ClassName { get; set; }

    public Guid SubjectId { get; set; }
    public string? SubjectName { get; set; }

    public Guid TeacherId { get; set; }
    public string? TeacherName { get; set; }

    public bool IsPublished { get; set; }

    public AdminAssignmentSubmissionDto? Submission { get; set; }
}