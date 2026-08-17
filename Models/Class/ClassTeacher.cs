using UserManagementApi.Models;

public class ClassTeacher
{
    public Guid Id { get; set; }

    public Guid SchoolId { get; set; }

    public Guid TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;

    public Guid ClassId { get; set; }
    public Class Class { get; set; } = null!;
}