namespace UserManagementApi.DTOs.Subjects;

public class AssignSubjectToClassesRequest
{
    public List<Guid> ClassIds { get; set; } = new();

    public Guid? TeacherId { get; set; }
}