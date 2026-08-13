namespace UserManagementApi.DTOs.Teachers;

public class CreateTeacherRequest
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string EmployeeNumber { get; set; } = string.Empty;
}