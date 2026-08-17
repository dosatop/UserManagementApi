namespace UserManagementApi.DTOs.Parents;

public class UpdateParentRequest
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }
}