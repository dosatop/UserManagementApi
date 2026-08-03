using Resend;

namespace UserManagementApi.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlContent);
}

public class EmailService(IResend resend, IConfiguration configuration, ILogger<EmailService> logger) : IEmailService
{
    private readonly IResend _resend = resend;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<EmailService> _logger = logger;

    public async Task SendEmailAsync(string to, string subject, string htmlContent)
    {
        try
        {
            var email = new EmailMessage
            {
                From = _configuration["Resend:From"]!,
                To = to,
                Subject = subject,
                HtmlBody = htmlContent
            };

            email.To.Add(to);

            await _resend.EmailSendAsync(email);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email to {Email}",
                to);

            throw; // Let the controller/global handler deal with it
        }
    }
}
