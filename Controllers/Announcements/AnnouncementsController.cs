using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementApi.DTOs.Announcements;
using UserManagementApi.Services.Interfaces;

namespace UserManagementApi.Controllers;

// ====================================================================
// ANNOUNCEMENTS — every signed-in user
// ====================================================================
//   GET    api/announcements?page=1&pageSize=20&unreadOnly=false&mine=false
//   GET    api/announcements/unread-count
//   GET    api/announcements/{id}
//   POST   api/announcements/{id}/read
//   POST   api/announcements/read-all
//
//   GET    api/announcements/compose-options     (admin, super admin, teacher)
//   POST   api/announcements                      (admin, super admin, teacher)
//   PUT    api/announcements/{id}                 (author / school admin / super admin)
//   DELETE api/announcements/{id}                 (author / school admin / super admin)
//
// Role checks live in AnnouncementService (it knows the user's school
// and classes), so the controller only needs [Authorize].

[ApiController]
[Route("api/announcements")]
[Authorize]
public class AnnouncementsController(IAnnouncementService service) : ControllerBase
{
    private readonly IAnnouncementService _service = service;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ---- reading -------------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> GetFeed(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool unreadOnly = false,
        [FromQuery] bool mine = false) =>
        ToResponse(await _service.GetFeedAsync(UserId, page, pageSize, unreadOnly, mine));

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount() =>
        ToResponse(await _service.GetUnreadCountAsync(UserId));

    [HttpGet("compose-options")]
    public async Task<IActionResult> GetComposeOptions() =>
        ToResponse(await _service.GetComposeOptionsAsync(UserId));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id) =>
        ToResponse(await _service.GetAnnouncementAsync(UserId, id));

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id) =>
        ToResponse(await _service.MarkReadAsync(UserId, id));

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead() =>
        ToResponse(await _service.MarkAllReadAsync(UserId));

    // ---- writing -------------------------------------------------------

    [HttpPost]
    public async Task<IActionResult> Create(CreateAnnouncementRequest request) =>
        ToResponse(await _service.CreateAsync(UserId, request));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateAnnouncementRequest request) =>
        ToResponse(await _service.UpdateAsync(UserId, id, request));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id) =>
        ToResponse(await _service.DeleteAsync(UserId, id));

    // ---- result → HTTP -------------------------------------------------

    private IActionResult ToResponse(AnnouncementResult result) => result.Status switch
    {
        200 => Ok(result.Data),
        404 => NotFound(new { message = result.Error }),
        403 => StatusCode(403, new { message = result.Error }),
        _ => BadRequest(new { message = result.Error })
    };
}

// ====================================================================
// PUSH DEVICES — every signed-in user
// ====================================================================
//   POST   api/devices           { token, platform }   (after login / token refresh)
//   DELETE api/devices/{token}                          (on logout)

[ApiController]
[Route("api/devices")]
[Authorize]
public class DevicesController(IAnnouncementService service) : ControllerBase
{
    private readonly IAnnouncementService _service = service;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost]
    public async Task<IActionResult> Register(RegisterDeviceRequest request)
    {
        var result = await _service.RegisterDeviceAsync(UserId, request);
        return result.Success ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpDelete("{token}")]
    public async Task<IActionResult> Unregister(string token)
    {
        var result = await _service.UnregisterDeviceAsync(UserId, token);
        return Ok(result.Data);
    }
}
