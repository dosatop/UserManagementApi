using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.DTOs.Trades;
using UserManagementApi.Models;
using UserManagementApi.Models.SchoolModels;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Services;

public class TradeService : ITradeService
{
    private readonly ApplicationDbContext _context;

    public TradeService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // CREATE
    // ============================================================

    public async Task<(bool Success, object? Data, string? Error)>
        CreateTradeAsync(
            Guid schoolId,
            CreateTradeRequest request)
    {
        var school = await _context.Schools
            .FirstOrDefaultAsync(x => x.Id == schoolId);

        if (school == null)
        {
            return (
                false,
                null,
                "School not found."
            );
        }

        var name = request.Name.Trim();
        var code = request.Code?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return (
                false,
                null,
                "Trade name is required."
            );
        }

        var exists = await _context.Trades
            .AnyAsync(x =>
                x.SchoolId == schoolId &&
                x.Name.ToLower() == name.ToLower());

        if (exists)
        {
            return (
                false,
                null,
                "A trade with this name already exists."
            );
        }

        if (!string.IsNullOrWhiteSpace(code))
        {
            var codeExists = await _context.Trades
                .AnyAsync(x =>
                    x.SchoolId == schoolId &&
                    x.Code != null &&
                    x.Code.ToLower() == code.ToLower());

            if (codeExists)
            {
                return (
                    false,
                    null,
                    "A trade with this code already exists."
                );
            }
        }

        var trade = new Trade
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Name = name,
            Code = code
        };

        _context.Trades.Add(trade);

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                trade.Id,
                trade.Name,
                trade.Code,
                trade.SchoolId,
                SchoolName = school.Name
            },
            null
        );
    }

    // ============================================================
    // GET ALL
    // ============================================================

    public async Task<IEnumerable<object>>
        GetTradesAsync(Guid schoolId)
    {
        return await _context.Trades
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Code,
                x.SchoolId
            })
            .ToListAsync();
    }

    // ============================================================
    // GET ONE
    // ============================================================

    public async Task<(bool Success, object? Data, string? Error)>
        GetTradeAsync(
            Guid schoolId,
            Guid tradeId)
    {
        var trade = await _context.Trades
            .AsNoTracking()
            .Where(x =>
                x.Id == tradeId &&
                x.SchoolId == schoolId)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Code,
                x.SchoolId
            })
            .FirstOrDefaultAsync();

        if (trade == null)
        {
            return (
                false,
                null,
                "Trade not found."
            );
        }

        return (
            true,
            trade,
            null
        );
    }

    // ============================================================
    // UPDATE
    // ============================================================

    public async Task<(bool Success, object? Data, string? Error)>
        UpdateTradeAsync(
            Guid schoolId,
            Guid tradeId,
            CreateTradeRequest request)
    {
        var trade = await _context.Trades
            .FirstOrDefaultAsync(x =>
                x.Id == tradeId &&
                x.SchoolId == schoolId);

        if (trade == null)
        {
            return (
                false,
                null,
                "Trade not found."
            );
        }

        var name = request.Name.Trim();
        var code = request.Code?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return (
                false,
                null,
                "Trade name is required."
            );
        }

        var nameExists = await _context.Trades
            .AnyAsync(x =>
                x.SchoolId == schoolId &&
                x.Id != tradeId &&
                x.Name.ToLower() == name.ToLower());

        if (nameExists)
        {
            return (
                false,
                null,
                "A trade with this name already exists."
            );
        }

        if (!string.IsNullOrWhiteSpace(code))
        {
            var codeExists = await _context.Trades
                .AnyAsync(x =>
                    x.SchoolId == schoolId &&
                    x.Id != tradeId &&
                    x.Code != null &&
                    x.Code.ToLower() == code.ToLower());

            if (codeExists)
            {
                return (
                    false,
                    null,
                    "A trade with this code already exists."
                );
            }
        }

        trade.Name = name;
        trade.Code = code;

        await _context.SaveChangesAsync();

        return (
            true,
            new
            {
                trade.Id,
                trade.Name,
                trade.Code,
                trade.SchoolId
            },
            null
        );
    }

    // ============================================================
    // DELETE
    // ============================================================

    public async Task<(bool Success, string? Error)>
        DeleteTradeAsync(
            Guid schoolId,
            Guid tradeId)
    {
        var trade = await _context.Trades
            .FirstOrDefaultAsync(x =>
                x.Id == tradeId &&
                x.SchoolId == schoolId);

        if (trade == null)
        {
            return (
                false,
                "Trade not found."
            );
        }

        _context.Trades.Remove(trade);

        await _context.SaveChangesAsync();

        return (true, null);
    }
}
