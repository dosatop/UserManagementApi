namespace UserManagementApi.DTOs.Results;

public class CreateResultRequest
{
    public Guid StudentId { get; set; }

    public Guid SubjectId { get; set; }

    public Guid ClassId { get; set; }

    public string Session { get; set; } = string.Empty;

    public string Term { get; set; } = string.Empty;

    public decimal Score { get; set; }

    public decimal? ExamScore { get; set; }

    public decimal? TestScore { get; set; }

    public string? Remark { get; set; }
}