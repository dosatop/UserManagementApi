using UserManagementApi.Models.AuthModels;

namespace UserManagementApi.Models
{
    public class LoginResponse
    {
        public required string Id { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public required string Email { get; set; }

        public required TokenResponse TokenResponse { get; set;}
    }
}