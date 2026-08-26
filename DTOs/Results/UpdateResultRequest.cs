using System.ComponentModel.DataAnnotations;

namespace UserManagementApi.DTOs.Results;
public class UpdateResultRequest
{
    [Range(0, 40)]
    public decimal TestScore { get; set; }

    [Range(0, 60)]
    public decimal ExamScore { get; set; }

    public string? Remark { get; set; }
}
