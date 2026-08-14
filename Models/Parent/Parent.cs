using UserManagementApi.Models.SchoolModels;

namespace UserManagementApi.Models;

public class Parent
{
    public Guid Id { get; set; }

    // ============================================================
    // LOGIN ACCOUNT
    // ============================================================

    public string UserId { get; set; } = string.Empty;

    public User User { get; set; } = null!;

    // ============================================================
    // SCHOOL
    // ============================================================

    public Guid SchoolId { get; set; }

    public School School { get; set; } = null!;

    // ============================================================
    // CHILDREN
    // ============================================================

    public ICollection<ParentStudent> Children { get; set; } = [];
}