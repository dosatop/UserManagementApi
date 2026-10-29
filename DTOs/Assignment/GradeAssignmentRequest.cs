
using System.ComponentModel.DataAnnotations;

public class GradeAssignmentRequest
{
    [Required]
    [Range(0, 10, ErrorMessage = "Score must be between 0 and 10.")]
    public decimal Score { get; set; }

    [MaxLength(2000)]
    public string? Feedback { get; set; }
}
