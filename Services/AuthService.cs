using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.Models;
using UserManagementApi.Models.AuthModels;
using UserManagementApi.Models.ErrorModels;
using UserManagementApi.Models.SchoolModels;

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
        string? schoolName = null;
        Guid? parentId = null;

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
            var parent = await _context.Parents
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (parent == null)
            {
                _logger.LogError(
                    "PARENT PROFILE NOT FOUND. UserId: {UserId}",
                    user.Id
                );

                return ServiceResult<LoginResponse>.Failure([
                    new ServiceError
            {
                Code = "PARENT_PROFILE_NOT_FOUND",
                Message = "Parent profile not found."
            }
                ]);
            }

            parentId = parent.ParentId;
            schoolId = parent.SchoolId;

            _logger.LogInformation(
                "PARENT FOUND. ParentId: {ParentId}, UserId: {UserId}, SchoolId: {SchoolId}",
                parent.ParentId,
                parent.UserId,
                parent.SchoolId
            );

        }
        // Get school name separately
        if (schoolId.HasValue)
        {
            schoolName = await _context.Schools
                .AsNoTracking()
                .Where(x => x.Id == schoolId.Value)
                .Select(x => x.Name)
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
                SchoolName = schoolName,
                ParentId = parentId,

                TokenResponse = new TokenResponse
                {
                    AccessToken = accessToken.AccessToken,
                    RefreshToken = refreshToken.Token,
                    ExpiresAt = accessToken.ExpiresAt,
                    ExpiresIn = accessToken.ExpiresIn
                }
            });
    }
    // ================================================================
    // CREATE STUDENT
    // ================================================================
    // Creates the login account (email + password) and the student
    // profile together. If anything fails, nothing is saved.
    // ================================================================

    public async Task<(bool Success, object? Data, string? Error)>
        CreateStudentAsync(
            Guid schoolId,
            CreateStudentRequest request)
    {
        // ============================================================
        // CLEAN INPUT
        // ============================================================
        // Email is trimmed. Password is NOT trimmed — it must be saved
        // exactly as typed so login matches.

        var email = request.Email?.Trim() ?? string.Empty;
        var fullName = request.FullName?.Trim() ?? string.Empty;
        var studentNumber = request.StudentNumber?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(fullName))
            return (false, null, "Full name is required.");

        if (string.IsNullOrWhiteSpace(email))
            return (false, null, "Email is required.");

        if (string.IsNullOrWhiteSpace(password))
            return (false, null, "Password is required.");

        if (string.IsNullOrWhiteSpace(studentNumber))
            return (false, null, "Student number is required.");

        // ============================================================
        // SCHOOL
        // ============================================================

        var school = await _context.Schools
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == schoolId);

        if (school == null)
            return (false, null, "School not found.");

        // ============================================================
        // CLASS
        // ============================================================

        var classroom = await _context.Classes
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == request.ClassRoomId &&
                x.SchoolId == schoolId);

        if (classroom == null)
            return (false, null, "Class does not belong to this school.");

        // ============================================================
        // DEPARTMENT
        // ============================================================

        Department? department = null;

        if (request.DepartmentId.HasValue)
        {
            department = await _context.Departments
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == request.DepartmentId.Value &&
                    x.SchoolId == schoolId);

            if (department == null)
                return (false, null, "Department does not belong to this school.");
        }

        // Senior students MUST have a department
        if (classroom.Level == SchoolLevel.Senior &&
            department == null)
        {
            return (false, null, "A department is required for Senior students.");
        }

        // Junior students CANNOT have a department
        if (classroom.Level == SchoolLevel.Junior &&
            department != null)
        {
            return (false, null, "Junior students cannot be assigned to a department.");
        }

        // ============================================================
        // DUPLICATES
        // ============================================================

        var studentNumberExists = await _context.StudentProfiles
            .AnyAsync(x =>
                x.SchoolId == schoolId &&
                x.StudentNumber == studentNumber);

        if (studentNumberExists)
            return (false, null, "A student with this student number already exists in this school.");

        if (await _userManager.FindByEmailAsync(email) != null)
            return (false, null, "An account with this email already exists.");

        // ============================================================
        // CREATE LOGIN + PROFILE (one transaction)
        // ============================================================

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // --------------------------------------------------------
            // LOGIN ACCOUNT
            // --------------------------------------------------------

            var user = new User
            {
                UserName = email,         // login looks up by email
                Email = email,
                EmailConfirmed = true,    // created by the school
                FullName = fullName,
                SchoolId = schoolId,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _userManager.CreateAsync(user, password);

            if (!created.Succeeded)
            {
                await transaction.RollbackAsync();

                // e.g. "Passwords must have at least one digit."
                var reason = string.Join(" ",
                    created.Errors.Select(e => e.Description));

                return (false, null, string.IsNullOrWhiteSpace(reason)
                    ? "Unable to create the student's login."
                    : reason);
            }

            var roleResult =
                await _userManager.AddToRoleAsync(user, Roles.Student);

            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return (false, null, "Unable to assign the Student role.");
            }

            // --------------------------------------------------------
            // STUDENT PROFILE
            // --------------------------------------------------------

            var studentProfile = new StudentProfile
            {
                Id = Guid.NewGuid(),

                UserId = user.Id,
                SchoolId = schoolId,

                StudentNumber = studentNumber,

                ClassId = classroom.Id,
                SchoolLevel = classroom.Level,

                // Senior = required, Junior = null
                DepartmentId = department?.Id,

                // Trade is chosen later
                TradeId = null,
                TradeSubjectId = null
            };

            _context.StudentProfiles.Add(studentProfile);

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            // ========================================================
            // RESPONSE
            // ========================================================

            return (
                true,
                new
                {
                    studentId = studentProfile.Id,
                    userId = user.Id,

                    fullName = user.FullName,
                    studentName = user.FullName,
                    email = user.Email,
                    phoneNumber = user.PhoneNumber,

                    studentNumber = studentProfile.StudentNumber,

                    classId = classroom.Id,
                    className = classroom.Name,

                    schoolLevel = classroom.Level.ToString(),
                    schoolLevelId = (int)classroom.Level,

                    departmentId = department?.Id,
                    departmentName = department?.Name,

                    tradeId = (Guid?)null,
                    tradeName = (string?)null,

                    schoolId = school.Id,
                    schoolName = school.Name,

                    parentCount = 0
                },
                null
            );
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return (false, null, "Unable to create student. Please try again.");
        }
    }
}