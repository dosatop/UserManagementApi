using System.ComponentModel.DataAnnotations;

namespace UserManagementApi.DTOs.Results;

// ====================================================================
// SINGLE / MANUAL EXAM RESULT
// ====================================================================

public class UploadExamResultRequest
{
    [Required]
    public Guid StudentId { get; set; }

    [Required]
    public Guid ClassId { get; set; }

    [Required]
    public Guid SubjectId { get; set; }

    // Exam is out of 60
    [Range(0, 60)]
    public decimal ExamScore { get; set; }

    public string? Remark { get; set; }
}



// ====================================================================
// BULK EXAM RESULT
// ====================================================================

public class BulkExamResultRequest
{
    [Required]
    public Guid ClassId { get; set; }

    [Required]
    public Guid SubjectId { get; set; }

    [Required]
    [MinLength(1)]
    public List<BulkExamResultItem> Results { get; set; } = new();
}


// ====================================================================
// BULK EXAM RESULT ITEM
// ====================================================================

public class BulkExamResultItem
{
    [Required]
    public Guid StudentId { get; set; }

    // Exam is out of 60
    [Range(0, 60)]
    public decimal ExamScore { get; set; }

    public string? Remark { get; set; }
}


public class UpdateExamResultRequest
{
    [Range(0, 60)]
    public decimal ExamScore { get; set; }

    public string? Remark { get; set; }
}
