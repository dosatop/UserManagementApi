namespace UserManagementApi.DTOs.Auth

{
    public class LoginRequest
    {
        public required string Email { get; set; } = string.Empty;
        public required string Password { get; set; } = string.Empty;
    }
}