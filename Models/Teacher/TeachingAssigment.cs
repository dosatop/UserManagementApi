using UserManagementApi.Models;
using UserManagementApi.Models.SchoolModels;

public class TeachingAssignment
{
    public Guid Id { get; set; }

    public Guid SchoolId { get; set; }
    public School School { get; set; } = null!;

    public Guid TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;

    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public Guid ClassId { get; set; }
    public Class Class { get; set; } = null!;
}