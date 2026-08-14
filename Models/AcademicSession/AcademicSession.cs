using UserManagementApi.Models.SchoolModels;

namespace UserManagementApi.Models;

public class AcademicSession
{
    public Guid Id { get; set; }

    public Guid SchoolId { get; set; }

    public School School { get; set; } = null!;

    public string Session { get; set; } = string.Empty;

    public string Term { get; set; } = string.Empty;

    public bool IsCurrent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}