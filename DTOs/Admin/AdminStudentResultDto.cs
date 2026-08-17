public class AdminStudentResultDto
{
    public Guid Id { get; set; }

    public Guid SubjectId { get; set; }
    public string? SubjectName { get; set; }

    public decimal? TestScore { get; set; }
    public decimal? ExamScore { get; set; }
    public decimal Score { get; set; }

    public string? Grade { get; set; }
    public string? Remark { get; set; }

    public string? Session { get; set; }
    public string? Term { get; set; }
}