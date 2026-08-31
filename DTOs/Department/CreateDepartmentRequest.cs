namespace UserManagementApi.DTOs.Departments;

public class CreateDepartmentRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }
}
