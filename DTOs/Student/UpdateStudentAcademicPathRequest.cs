namespace UserManagementApi.DTOs.Students;

public class UpdateStudentAcademicPathRequest
{
    public Guid? DepartmentId { get; set; }

    public Guid? TradeId { get; set; }
}
