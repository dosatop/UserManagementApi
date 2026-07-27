namespace UserManagementApi.Models.AuthModels;

public class RefreshToken
{
    public int Id { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    // Relationship with Identity User
    public string UserId { get; set; } = string.Empty;

    public User User { get; set; } = null!;
}