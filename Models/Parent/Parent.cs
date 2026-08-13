using UserManagementApi.Models.SchoolModels;

namespace UserManagementApi.Models;

public class Parent
{
    public Guid Id { get; set; }

    // Login account
    public string UserId { get; set; } = string.Empty;

    public User User { get; set; } = null!;

    // School
    public Guid SchoolId { get; set; }

    public School School { get; set; } = null!;

    public ICollection<ParentStudent> Children { get; set; } = [];
}