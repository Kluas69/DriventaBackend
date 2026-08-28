namespace Driventa.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendPasswordResetEmailAsync(string to, string resetLink);
}
