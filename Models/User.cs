using Microsoft.AspNetCore.Identity;
using UserManagementApi.Models.AuthModels;

namespace UserManagementApi.Models;

public class User : IdentityUser
{
    public string? FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RefreshToken> RefreshTokens { get; set; }
        = [];
}