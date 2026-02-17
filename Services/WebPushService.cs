using WebPush;

namespace WebPing.Services;

public interface IWebPushService
{
    Task SendNotificationAsync(string endpoint, string p256dh, string auth, string payload);
}

public class WebPushService : IWebPushService
{
    private readonly IConfiguration _configuration;
    private readonly WebPushClient _webPushClient;

    public WebPushService(IConfiguration configuration)
    {
        _configuration = configuration;
        _webPushClient = new WebPushClient();

        var vapidPublicKey = _configuration["VapidKeys:PublicKey"];
        var vapidPrivateKey = _configuration["VapidKeys:PrivateKey"];
        var vapidSubject = _configuration["VapidKeys:Subject"] ?? "mailto:example@example.com";

        if (!string.IsNullOrEmpty(vapidPublicKey) && !string.IsNullOrEmpty(vapidPrivateKey))
        {
            _webPushClient.SetVapidDetails(vapidSubject, vapidPublicKey, vapidPrivateKey);
        }
    }

    public async Task SendNotificationAsync(string endpoint, string p256dh, string auth, string payload)
    {
        var subscription = new PushSubscription(endpoint, p256dh, auth);
        try
        {
            await _webPushClient.SendNotificationAsync(subscription, payload);
        }
        catch (WebPushException ex)
        {
            // Log the error or handle it appropriately
            throw new Exception($"Failed to send push notification: {ex.Message}", ex);
        }
    }
}
