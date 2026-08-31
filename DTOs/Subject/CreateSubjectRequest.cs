using UserManagementApi.Models;

namespace UserManagementApi.DTOs.Subjects;

public class CreateSubjectRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }

    public SubjectType Type { get; set; }

    public Guid? DepartmentId { get; set; }

    public Guid? TradeId { get; set; }
}
