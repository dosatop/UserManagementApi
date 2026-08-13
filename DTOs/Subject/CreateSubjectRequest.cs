namespace UserManagementApi.DTOs.Subjects;

public class CreateSubjectRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }
}