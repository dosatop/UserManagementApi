namespace UserManagementApi.DTOs.Trades;

public class CreateTradeRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }
}
