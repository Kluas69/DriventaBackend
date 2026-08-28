using Driventa.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Driventa.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string body)
    {
        _logger.LogInformation("Sending email to {To} with subject: {Subject}", to, subject);
        _logger.LogDebug("Email body: {Body}", body);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string to, string resetLink)
    {
        _logger.LogInformation("Sending password reset email to {To}", to);
        _logger.LogDebug("Password reset link: {ResetLink}", resetLink);
        return Task.CompletedTask;
    }
}
