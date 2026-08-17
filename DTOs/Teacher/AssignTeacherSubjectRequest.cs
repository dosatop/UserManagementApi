namespace UserManagementApi.DTOs.Teachers;

public class AssignTeacherSubjectRequest
{
    public Guid SubjectId { get; set; }

    public Guid ClassId { get; set; }
}