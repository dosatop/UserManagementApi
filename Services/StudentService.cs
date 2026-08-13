using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Auth.Roles;
using UserManagementApi.Services.Interfaces;

public class StudentService : IStudentService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserManagementService _userManagementService;

    public StudentService(
        ApplicationDbContext context,
        IUserManagementService userManagementService)
    {
        _context = context;
        _userManagementService = userManagementService;
    }

    public async Task<(bool Success, object? Data, string? Error)>
        CreateStudentAsync(
            Guid schoolId,
            CreateStudentRequest request)
    {
        var school = await _context.Schools
            .FirstOrDefaultAsync(x => x.Id == schoolId);

        if (school == null)
        {
            return (false, null, "School not found.");
        }

        var classroom = await _context.Classes
            .FirstOrDefaultAsync(x =>
                x.Id == request.ClassRoomId &&
                x.SchoolId == schoolId);

        if (classroom == null)
        {
            return (
                false,
                null,
                "Class does not belong to this school."
            );
        }

        var (Success, User, Error) =
            await _userManagementService.CreateUserAsync(
                request.FullName,
                request.Email,
                request.Password,
                Roles.Student);

        if (!Success)
        {
            return (
                false,
                null,
                Error
            );
        }

        var user = User!;

        var studentProfile = new StudentProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SchoolId = schoolId,
            StudentNumber = request.StudentNumber,
            ClassId = request.ClassRoomId
        };

        _context.StudentProfiles.Add(studentProfile);

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                user.Id,
                user.FullName,
                user.Email,
                studentProfile.StudentNumber,
                Class = classroom.Name,
                School = school.Name
            },
            null
        );
    }

    public async Task<IEnumerable<object>> GetStudentsAsync(
    Guid schoolId)
    {
        return await _context.StudentProfiles
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.StudentNumber,
                FullName = x.User.FullName,
                Email = x.User.Email,
                Class = x.Class.Name
            })
            .ToListAsync();
    }
}