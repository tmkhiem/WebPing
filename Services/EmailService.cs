using Google.Apis.Auth.OAuth2;
using WebPing.Utilities;

namespace WebPing.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly UserCredential _credential;

    public EmailService(ILogger<EmailService> logger, UserCredential credential)
    {
        _logger = logger;
        _credential = credential;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        _logger.LogInformation("Sending email to {To} with subject '{Subject}'", to, subject);
        
        try 
        {
            await Task.Run(() => MailUtilities.SendEmail(_credential, to, subject, body));
            _logger.LogInformation("Email sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            throw;
        }
    }
}
