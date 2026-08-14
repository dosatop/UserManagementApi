using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.Models;
using UserManagementApi.Models.AuthModels;
using UserManagementApi.Models.ErrorModels;

namespace UserManagementApi.Services;

public class AuthService(
    UserManager<User> userManager,
    TokenService tokenService, ApplicationDbContext context, RolesService roleService, ILogger<AuthService> _logger)
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly TokenService _tokenService = tokenService;
    private readonly ApplicationDbContext _context = context;
    private readonly RolesService _roleService = roleService;

    public async Task<ServiceResult<LoginResponse>> LoginAsync(
      string email,
      string password)
    {
        _logger.LogInformation("Login attempt");

        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
            return ServiceResult<LoginResponse>.Failure([
                new ServiceError
            {
                Code = AuthErrorCodes.InvalidCredentials,
                Message = "Invalid email or password"
            }
            ]);

        var isPasswordValid =
            await _userManager.CheckPasswordAsync(user, password);

        if (!isPasswordValid)
            return ServiceResult<LoginResponse>.Failure([
                new ServiceError
            {
                Code = AuthErrorCodes.InvalidCredentials,
                Message = "Invalid email or password"
            }
            ]);

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault();

        Guid? schoolId = null;

        if (role == Roles.Admin)
        {
            schoolId = await _context.Users
                .AsNoTracking()
                .Where(x => x.Id == user.Id)
                .Select(x => (Guid?)x.SchoolId)
                .FirstOrDefaultAsync();
        }
        else if (role == Roles.Student)
        {
            schoolId = await _context.StudentProfiles
                .AsNoTracking()
                .Where(x => x.UserId == user.Id)
                .Select(x => (Guid?)x.SchoolId)
                .FirstOrDefaultAsync();
        }
        else if (role == Roles.Teacher)
        {
            schoolId = await _context.Teachers
                .AsNoTracking()
                .Where(x => x.UserId == user.Id)
                .Select(x => (Guid?)x.SchoolId)
                .FirstOrDefaultAsync();
        }
        else if (role == Roles.Parent)
        {
            schoolId = await _context.Parents
                .AsNoTracking()
                .Where(x => x.UserId == user.Id)
                .Select(x => (Guid?)x.SchoolId)
                .FirstOrDefaultAsync();
        }

        var accessToken =
            await _tokenService.CreateAccessToken(user);

        var refreshToken = new RefreshToken
        {
            Token = _tokenService.GenerateRefreshToken(),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _context.RefreshTokens.Add(refreshToken);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "User {UserId} logged in successfully",
            user.Id);

        return ServiceResult<LoginResponse>.Success(
            new LoginResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber!,
                Role = role!,
                SchoolId = schoolId,

                TokenResponse = new TokenResponse
                {
                    AccessToken = accessToken.AccessToken,
                    RefreshToken = refreshToken.Token,
                    ExpiresAt = accessToken.ExpiresAt,
                    ExpiresIn = accessToken.ExpiresIn
                }
            });
    }

    public async Task<ServiceResult<User>> CreateUserAsync(
        string email,
        string password,
        string fullName,
        string phoneNumber, string? userName)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var existingUser = await _userManager.FindByEmailAsync(email);

            if (existingUser is not null)
            {
                await transaction.RollbackAsync();

                return ServiceResult<User>.Failure(
                    [new ServiceError
                {
                    Code = AuthErrorCodes.EmailAlreadyRegistered,
                    Message = "Email already exist"
                }
                    ]);
            }

            User user = new()
            {
                UserName = userName ?? email,
                Email = email,
                FullName = fullName,
                PhoneNumber = phoneNumber,
                CreatedAt = DateTime.UtcNow,
            };


            var result = await _userManager
                .CreateAsync(user, password);

            if (!result.Succeeded)
            {
                await transaction.RollbackAsync();

                foreach (var error in result.Errors)
                {
                    _logger.LogWarning("User creation failed. Code {Code}, Description: {Description}",
                    error.Code,
                    error.Description
                    );
                }

                return ServiceResult<User>.Failure(
                   [
                    new ServiceError
                {
                    Code = AuthErrorCodes.UserCreationFailed,
                    Message = "Unable to create user."
                }
                   ]);
            }

            var roleResult = await _roleService.AssignRoleToUserAsync(user, Roles.Student);

            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync();

                _logger.LogError(
                    "User {UserId} was created but assigning role {Role} failed",
                    user.Id,
                    Roles.Student);

                return ServiceResult<User>.Failure([
                   new ServiceError{
                   Code = AuthErrorCodes.UserCreationFailed,
                   Message = "Unable to assign default role to user"
               }
                ]);
            }

            await transaction.CommitAsync();

            return ServiceResult<User>.Success(user);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "An error occurred while creating user {Email}", email); return ServiceResult<User>.Failure([new ServiceError { Code = AuthErrorCodes.UserCreationFailed, Message = "Unable to create user." }]);
        }
    }
}