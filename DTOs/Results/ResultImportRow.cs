namespace UserManagementApi.DTOs.Results;

public class ResultImportRow
{
    public int RowNumber { get; set; }

    public string StudentNumber { get; set; } = string.Empty;

    public string StudentName { get; set; } = string.Empty;

    public decimal? TestScore { get; set; }

    public decimal? ExamScore { get; set; }

    public decimal? Score { get; set; }

    public string? Remark { get; set; }

    public bool IsValid { get; set; }

    public List<string> Errors { get; set; } = [];
}