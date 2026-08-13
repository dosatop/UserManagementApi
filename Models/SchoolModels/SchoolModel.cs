namespace UserManagementApi.Models.SchoolModels;
public class School
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Address { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Users belonging to this school
    public ICollection<User> Users { get; set; } = [];

    // School classes
    public ICollection<Class> Classes { get; set; } = [];

    // School subjects
    public ICollection<Subject> Subjects { get; set; } = [];
}