using UserManagementApi.Models;

public class AcademicTerm
{
    public Guid Id { get; set; }

    public Guid AcademicSessionId { get; set; }

    public string Term { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsCurrent { get; set; }

    public DateTime CreatedAt { get; set; }

    public AcademicSession AcademicSession { get; set; } = null!;
}
