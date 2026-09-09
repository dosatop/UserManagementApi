using UserManagementApi.Models.SchoolModels;

namespace UserManagementApi.Models;

public class AcademicSession
{
    public Guid Id { get; set; }

    public Guid SchoolId { get; set; }

    public School School { get; set; } = null!;

    public string Session { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsCurrent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ================================================================
    // ACADEMIC TERMS
    // ================================================================

    public ICollection<AcademicTerm> Terms { get; set; }
        = new List<AcademicTerm>();
}