namespace UserManagementApi.DTOs.Students;

public class UpdateStudentRequest
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string StudentNumber { get; set; } = string.Empty;

    public Guid ClassRoomId { get; set; }
}