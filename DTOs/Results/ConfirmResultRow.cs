namespace UserManagementApi.DTOs.Results;

public class ConfirmResultRow
{
    public string StudentNumber { get; set; } = string.Empty;

    public decimal TestScore { get; set; }

    public decimal ExamScore { get; set; }

    public string? Remark { get; set; }
}