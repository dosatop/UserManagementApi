using UserManagementApi.DTOs.Trades;

namespace UserManagementApi.Services.Interfaces;

public interface ITradeService
{
    Task<(bool Success, object? Data, string? Error)>
        CreateTradeAsync(
            Guid schoolId,
            CreateTradeRequest request);

    Task<IEnumerable<object>>
        GetTradesAsync(Guid schoolId);

    Task<(bool Success, object? Data, string? Error)>
        GetTradeAsync(
            Guid schoolId,
            Guid tradeId);

    Task<(bool Success, object? Data, string? Error)>
        UpdateTradeAsync(
            Guid schoolId,
            Guid tradeId,
            CreateTradeRequest request);

    Task<(bool Success, string? Error)>
        DeleteTradeAsync(
            Guid schoolId,
            Guid tradeId);
}
