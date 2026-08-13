using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using UserManagementApi.Models;
using UserManagementApi.Models.AuthModels;

namespace UserManagementApi.Services;

public class TokenService(IConfiguration configuration, UserManager<User> userManager)
{
    private readonly IConfiguration _configuration = configuration;
    private readonly UserManager<User> _userManager = userManager;

    public async Task<AccessTokenResponse> CreateAccessToken(User user)
    {
       var claims = new List<Claim>
{
    new(ClaimTypes.NameIdentifier, user.Id),
    new(ClaimTypes.Email, user.Email ?? ""),
    new(ClaimTypes.Name, user.UserName ?? ""),
    new("uuid", Guid.NewGuid().ToString())
};

if (user.SchoolId.HasValue)
{
    claims.Add(
        new Claim(
            "SchoolId",
            user.SchoolId.Value.ToString()
        )
    );
}
        var roles = await _userManager.GetRolesAsync(user);

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"]!));

        var credentials =
            new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                Convert.ToDouble(
                    _configuration["Jwt:DurationInMinutes"])),
            signingCredentials: credentials);

        return new AccessTokenResponse
        {
            AccessToken = new JwtSecurityTokenHandler()
                .WriteToken(token),
            ExpiresAt = DateTime.UtcNow.AddMinutes(
                Convert.ToDouble(
                    _configuration["Jwt:DurationInMinutes"])),
            ExpiresIn = Convert.ToDouble(
                    _configuration["Jwt:DurationInMinutes"])
        };
    }

    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(64));
    }
}
