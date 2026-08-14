using UserManagementApi.Models.AuthModels;

namespace UserManagementApi.Models
{
    public class LoginResponse
    {
        public required string Id { get; set; }
        public Guid? ParentId { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = string.Empty;
        public required string Email { get; set; }
        public Guid? SchoolId { get; set; }
        public String? SchoolName { get; set; }
        public required TokenResponse TokenResponse { get; set; }
    }
}