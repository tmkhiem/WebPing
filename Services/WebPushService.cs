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
    private readonly bool _vapidConfigured;

    public WebPushService(IConfiguration configuration)
    {
        _configuration = configuration;
        _webPushClient = new WebPushClient();

        var vapidPublicKey = _configuration["VapidKeys:PublicKey"];
        var vapidPrivateKey = _configuration["VapidKeys:PrivateKey"];
        var vapidSubject = _configuration["VapidKeys:Subject"] ?? "mailto:example@example.com";

        // Only set VAPID details if keys are properly configured
        if (!string.IsNullOrEmpty(vapidPublicKey) && 
            !string.IsNullOrEmpty(vapidPrivateKey) &&
            vapidPublicKey != "YOUR_VAPID_PUBLIC_KEY" &&
            vapidPrivateKey != "YOUR_VAPID_PRIVATE_KEY")
        {
            try
            {
                _webPushClient.SetVapidDetails(vapidSubject, vapidPublicKey, vapidPrivateKey);
                _vapidConfigured = true;
            }
            catch
            {
                _vapidConfigured = false;
            }
        }
        else
        {
            _vapidConfigured = false;
        }
    }

    public async Task SendNotificationAsync(string endpoint, string p256dh, string auth, string payload)
    {
        if (!_vapidConfigured)
        {
            // For testing/demo purposes, just log that we would send a notification
            // In production, this should throw or require proper VAPID configuration
            Console.WriteLine($"[Demo Mode] Would send notification to {endpoint}");
            Console.WriteLine($"Payload: {payload}");
            return;
        }

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
