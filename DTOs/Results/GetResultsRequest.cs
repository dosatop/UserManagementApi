namespace UserManagementApi.DTOs.Results;

public class GetResultsRequest
{
    public Guid? StudentId { get; set; }

    public Guid? SubjectId { get; set; }

    public Guid? ClassId { get; set; }

    public string? Session { get; set; }

    public string? Term { get; set; }
}