namespace UserManagementApi.DTOs.Auth;

public class SignUpRequest
{
    public string? UserName { get; set; } = string.Empty;
    public required string FullName { get; set; } = string.Empty;
    public required string PhoneNumber { get; set; } = string.Empty;
    public required string Email { get; set; } = string.Empty;
    public required string Password { get; set; } = string.Empty;

}
