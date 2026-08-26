// ====================================================================
// SINGLE / MANUAL TEST RESULT
// ====================================================================

using System.ComponentModel.DataAnnotations;

public class UploadTestResultRequest
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

    public string? Remark { get; set; }
}


// ====================================================================
// BULK TEST RESULT
// ====================================================================

public class BulkTestResultRequest
{
    [Required]
    public Guid ClassId { get; set; }

    [Required]
    public Guid SubjectId { get; set; }

    [Required]
    [MinLength(1)]
    public List<BulkTestResultItem> Results { get; set; } = new();
}


// ====================================================================
// BULK TEST RESULT ITEM
// ====================================================================

public class BulkTestResultItem
{
    [Required]
    public Guid StudentId { get; set; }

    // Test is out of 40
    [Range(0, 40)]
    public decimal TestScore { get; set; }

    public string? Remark { get; set; }
}


public class UpdateTestResultRequest
{
    [Range(0, 40)]
    public decimal TestScore { get; set; }

    public string? Remark { get; set; }
}
