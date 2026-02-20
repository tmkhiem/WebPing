namespace WebPing.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }
    public Task SendEmailAsync(string to, string subject, string body)
    {
        // For demonstration purposes, we'll just log the email instead of sending it
        _logger.LogInformation("Sending email to {To} with subject '{Subject}' and body: {Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
