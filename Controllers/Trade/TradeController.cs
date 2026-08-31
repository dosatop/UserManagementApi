using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementApi.DTOs.Trades;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TradeController : ControllerBase
{
    private readonly ITradeService _tradeService;

    public TradeController(
        ITradeService tradeService)
    {
        _tradeService = tradeService;
    }

    // ================================================================
    // CREATE TRADE
    // ================================================================

    [HttpPost]
    public async Task<IActionResult> CreateTradeAsync(
        [FromBody] CreateTradeRequest req)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _tradeService.CreateTradeAsync(
            schoolId.Value,
            req
        );

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Trade created successfully.",
            data = result.Data
        });
    }


    // ================================================================
    // GET ALL TRADES
    // ================================================================

    [HttpGet]
    public async Task<IActionResult> GetTradesAsync()
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var trades = await _tradeService.GetTradesAsync(
            schoolId.Value
        );

        return Ok(trades);
    }


    // ================================================================
    // GET TRADE BY ID
    // ================================================================

    [HttpGet("{tradeId:guid}")]
    public async Task<IActionResult> GetTradeAsync(
        Guid tradeId)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _tradeService.GetTradeAsync(
            schoolId.Value,
            tradeId
        );

        if (!result.Success)
        {
            return NotFound(new
            {
                message = result.Error
            });
        }

        return Ok(result.Data);
    }


    // ================================================================
    // UPDATE TRADE
    // ================================================================

    [HttpPut("{tradeId:guid}")]
    public async Task<IActionResult> UpdateTradeAsync(
        Guid tradeId,
        [FromBody] CreateTradeRequest req)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _tradeService.UpdateTradeAsync(
            schoolId.Value,
            tradeId,
            req
        );

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Trade updated successfully.",
            data = result.Data
        });
    }


    // ================================================================
    // DELETE TRADE
    // ================================================================

    [HttpDelete("{tradeId:guid}")]
    public async Task<IActionResult> DeleteTradeAsync(
        Guid tradeId)
    {
        var schoolId = GetSchoolId();

        if (schoolId == null)
        {
            return BadRequest(new
            {
                message = "Admin account is not assigned to a school."
            });
        }

        var result = await _tradeService.DeleteTradeAsync(
            schoolId.Value,
            tradeId
        );

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Error
            });
        }

        return Ok(new
        {
            message = "Trade deleted successfully."
        });
    }


    // ================================================================
    // SCHOOL ID
    // ================================================================

    private Guid? GetSchoolId()
    {
        var claim = User.FindFirst("schoolId");

        if (claim == null)
        {
            return null;
        }

        if (Guid.TryParse(claim.Value, out var schoolId))
        {
            return schoolId;
        }

        return null;
    }
}
