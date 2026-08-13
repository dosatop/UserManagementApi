using UserManagementApi.Models;
using UserManagementApi.Models.SchoolModels;

public class StudentProfile
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public Class Class { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;

    public User User { get; set; } = null!;

    public Guid SchoolId { get; set; }

    public School School { get; set; } = null!;

    public string StudentNumber { get; set; } = string.Empty;

    public ICollection<ParentStudent> Parents { get; set; } = [];
}