// ====================================================================
// SINGLE / MANUAL TEST + EXAM RESULT
// ====================================================================

using System.ComponentModel.DataAnnotations;

public class UploadResultRequest
{
    [Required]
    public Guid StudentId { get; set; }

    [Required]
    public Guid ClassId { get; set; }

    [Required]
    public Guid SubjectId { get; set; }

    // Test is out of 40
    [Range(0, 40)]
    public decimal TestScore { get; set; }

    // Exam is out of 60
    [Range(0, 60)]
    public decimal ExamScore { get; set; }

    public string? Remark { get; set; }
}



// ====================================================================
// BULK TEST + EXAM RESULT
// ====================================================================

public class BulkResultRequest
{
    [Required]
    public Guid ClassId { get; set; }

    [Required]
    public Guid SubjectId { get; set; }

    [Required]
    [MinLength(1)]
    public List<BulkResultItem> Results { get; set; } = new();
}


// ====================================================================
// BULK TEST + EXAM RESULT ITEM
// ====================================================================

public class BulkResultItem
{
    [Required]
    public Guid StudentId { get; set; }

    // Test is out of 40
    [Range(0, 40)]
    public decimal TestScore { get; set; }

    // Exam is out of 60
    [Range(0, 60)]
    public decimal ExamScore { get; set; }

    public string? Remark { get; set; }
}

