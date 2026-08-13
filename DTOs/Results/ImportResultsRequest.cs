namespace UserManagementApi.DTOs.Results;

public class ImportResultsRequest
{
    public Guid ClassId { get; set; }

    public Guid SubjectId { get; set; }

    public string Session { get; set; } = string.Empty;

    public string Term { get; set; } = string.Empty;

    public IFormFile File { get; set; } = null!;
}