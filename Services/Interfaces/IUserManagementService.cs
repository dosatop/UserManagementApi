using UserManagementApi.Models;

namespace UserManagementApi.Services.Interfaces;

public interface IUserManagementService
{
    Task<(bool Success, User? User, string? Error)> CreateUserAsync(
        string fullName,
        string email,
        string password,
                string role);
}