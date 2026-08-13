using UserManagementApi.Models.SchoolModels;

namespace UserManagementApi.Models.Results;

public class StudentResult
{
    public Guid Id { get; set; }

    // Student
    public Guid StudentId { get; set; }

    public StudentProfile Student { get; set; } = null!;

    // School
    public Guid SchoolId { get; set; }

    public School School { get; set; } = null!;

    // Subject
    public Guid SubjectId { get; set; }

    public Subject Subject { get; set; } = null!;

    // Class
    public Guid ClassId { get; set; }

    public Class Class { get; set; } = null!;

    // Academic information
    public string Session { get; set; } = string.Empty;

    public string Term { get; set; } = string.Empty;

    // Score
    public decimal Score { get; set; }
     public string Grade { get; set; } = string.Empty;

    public decimal? ExamScore { get; set; }

    public decimal? TestScore { get; set; }

    // Optional teacher remark
    public string? Remark { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}