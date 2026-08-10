namespace UserManagementApi.Models.ErrorModels;

public class ServiceError
{
    public required string Code { get; init; }
    public required string Message { get; init; }
}

public static class AuthErrorCodes
{
    public const string InvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string EmailAlreadyRegistered = "AUTH_EMAIL_ALREADY_REGISTERED";
    public const string UserCreationFailed = "AUTH_USER_CREATION_FAILED";
    public const string RoleAssignmentFailed = "AUTH_ROLE_ASSIGNMENT_FAILED";

}