namespace UserManagementApi.DTOs.AcademicSessions;

public class CreateAcademicSessionRequest
{
    public string Session { get; set; } = string.Empty;

    public string Term { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}