namespace UserManagementApi.DTOs.Results;

public class ConfirmResultImportRequest
{
    public Guid ClassId { get; set; }

    public Guid SubjectId { get; set; }

    public string Session { get; set; } = string.Empty;

    public string Term { get; set; } = string.Empty;

    public List<ConfirmResultRow> Rows { get; set; } = [];
}