using Microsoft.AspNetCore.Identity;
using UserManagementApi.Data;
using UserManagementApi.Models;
using UserManagementApi.Models.AuthModels;

namespace UserManagementApi.Services;

public class AuthService(
    UserManager<User> userManager,
    TokenService tokenService, ApplicationDbContext context)
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly TokenService _tokenService = tokenService;
    private readonly ApplicationDbContext _context = context;

    public async Task<ServiceResult<TokenResponse>> LoginAsync(
        string email,
        string password)
    {

        var user = await _userManager
            .FindByEmailAsync(email);

        if (user is null)
            return ServiceResult<TokenResponse>.Failure(["Invalid email or password"]);


        var isPasswordValid =
            await _userManager
                .CheckPasswordAsync(user, password);

        if (!isPasswordValid)
            return ServiceResult<TokenResponse>.Failure(["Invalid email or password"]);

        var accessToken = await _tokenService.CreateAccessToken(user);

        var refreshToken = new RefreshToken
        {
            Token = _tokenService.GenerateRefreshToken(),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _context.RefreshTokens.Add(refreshToken);

        await _context.SaveChangesAsync();

        return ServiceResult<TokenResponse>.Success(
            new TokenResponse
            {
                AccessToken = accessToken.AccessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = accessToken.ExpiresAt,
                ExpiresIn = accessToken.ExpiresIn
            });
    }

    public async Task<ServiceResult<User>> CreateUserAsync(
        string email,
        string password,
        string fullName,
        string phoneNumber)
    {
        User user = new()
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            PhoneNumber = phoneNumber,
            CreatedAt = DateTime.UtcNow,
        };


        var result = await _userManager
            .CreateAsync(user, password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"{error.Code}: {error.Description}");
            }

            return ServiceResult<User>.Failure(
                result.Errors.Select(e => e.Description));
        }

        return ServiceResult<User>.Success(user);
    }
}