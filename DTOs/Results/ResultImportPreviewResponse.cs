namespace UserManagementApi.DTOs.Results;

public class ResultImportPreviewResponse
{
    public int TotalRows { get; set; }

    public int ValidRows { get; set; }

    public int InvalidRows { get; set; }

    public bool CanImport { get; set; }

    public List<ResultImportRow> Rows { get; set; } = [];
}