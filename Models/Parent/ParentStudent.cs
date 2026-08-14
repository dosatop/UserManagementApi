using UserManagementApi.Models;

public class ParentStudent
{
    public Guid ParentId { get; set; }

    public Guid StudentId { get; set; }

    public Parent Parent { get; set; } = null!;

    public StudentProfile Student { get; set; } = null!;
}