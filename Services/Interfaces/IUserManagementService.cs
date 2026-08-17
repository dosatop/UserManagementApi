using UserManagementApi.Models;

namespace UserManagementApi.Services.Interfaces;

public interface IUserManagementService
{
    Task<(bool Success, User? User, string? Error)> CreateUserAsync(
        string fullName,
        string email,
        string password,
        string phoneNumber,
        string? employeeNumber,
                string role);

    Task<(
bool Success,
User? User,
string? Error
)> CreateSchoolAdminAsync(
Guid schoolId,
string fullName,
string email,
string phoneNumber,
        string password);
}
