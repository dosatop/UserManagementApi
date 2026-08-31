using UserManagementApi.Models.SchoolModels;

namespace UserManagementApi.Models;

public class Trade
{
    public Guid Id { get; set; }

    public Guid SchoolId { get; set; }

    public School School { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }

    // Subjects belonging to this trade
    public ICollection<Subject> Subjects { get; set; } = [];
}
