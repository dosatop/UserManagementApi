using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Tree;
using UserManagementApi.Models.EmailModels;
using UserManagementApi.Services;

namespace UserManagementApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EmailController(
    IEmailService emailService
   ) : ControllerBase
{
    private readonly IEmailService _emailService = emailService;

    [HttpPost("send")]
    public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
    {
        try
        {
            await _emailService.SendEmailAsync(
                request.To,
                request.Subject,
                request.HtmlContent);

            return Ok(new
            {
                message = "Email successfully sent"
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                message = $"Unable to send email to {request.To}"
            });
        }
    }
}