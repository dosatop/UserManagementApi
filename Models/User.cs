using Microsoft.AspNetCore.Identity;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.Models.AuthModels;
using UserManagementApi.Models.SchoolModels;

namespace UserManagementApi.Models;

public class User : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? SchoolId { get; set; }
    public string? EmployeeNumber { get; set; }
    

    public School? School { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    public StudentProfile? Student { get; set; }

    public Teacher? Teacher { get; set; }

    public Parent? Parent { get; set; }
}