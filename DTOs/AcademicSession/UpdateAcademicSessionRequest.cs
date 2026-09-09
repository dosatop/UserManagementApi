namespace UserManagementApi.DTOs.AcademicSessions;

public class UpdateAcademicSessionRequest
{
public string Session { get; set; } = string.Empty;

public DateTime StartDate { get; set; }

public DateTime EndDate { get; set; }

}