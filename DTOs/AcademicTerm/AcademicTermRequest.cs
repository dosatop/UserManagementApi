namespace UserManagementApi.DTOs.AcademicTerms;

public class CreateAcademicTermRequest
{
    public Guid AcademicSessionId { get; set; }

    public string Term { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
    public bool IsCurrent { get; set; }
}

public class UpdateAcademicTermRequest
{
    public string Term { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
    public bool IsCurrent { get; set; }
}

