namespace UserManagementApi.DTOs.Results;

public class UpdateResultRequest
{
    public decimal Score { get; set; }

    public decimal? ExamScore { get; set; }

    public decimal? TestScore { get; set; }

    public string? Remark { get; set; }
}