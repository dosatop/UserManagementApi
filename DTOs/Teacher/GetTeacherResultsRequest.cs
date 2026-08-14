namespace UserManagementApi.DTOs.Results;

public class GetTeacherResultsRequest
{
    public Guid? ClassId { get; set; }

    public Guid? SubjectId { get; set; }

    public Guid? StudentId { get; set; }

    public string? Session { get; set; }

    public string? Term { get; set; }
}