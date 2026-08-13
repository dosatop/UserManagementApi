using UserManagementApi.Models;
using UserManagementApi.Models.SchoolModels;

public class ParentProfile
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public Guid SchoolId { get; set; }

    public User User { get; set; } = null!;

    public School School { get; set; } = null!;

      public ICollection<ParentStudent> Children { get; set; } = [];
}