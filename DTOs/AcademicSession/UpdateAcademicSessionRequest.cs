namespace UserManagementApi.DTOs.AcademicSessions;

public class UpdateAcademicSessionRequest
{
    public string Session { get; set; } = string.Empty;

    public string Term { get; set; } = string.Empty;
}