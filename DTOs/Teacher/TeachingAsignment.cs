namespace UserManagementApi.DTOs.Teachers;

public class AssignTeachingSubjectRequest
{
    public Guid SubjectId { get; set; }
    public Guid? ClassId { get; set; }
}