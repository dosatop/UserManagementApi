namespace UserManagementApi.DTOs.Teachers;

public class UpdateTeacherRequest
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string EmployeeNumber { get; set; } = string.Empty;
}