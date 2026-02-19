using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebPing.Data;
using WebPing.DTOs;
using WebPing.Models;
using WebPing.Services;

namespace WebPing.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/send/{topicName}", async (string topicName, SendNotificationRequest request, WebPingDbContext dbContext, IWebPushService webPushService) =>
        {
            // Find the topic
            var topic = await dbContext.Topics
                .Include(t => t.User)
                .ThenInclude(u => u!.PushEndpoints)
                .FirstOrDefaultAsync(t => t.Name == topicName);

            if (topic == null)
            {
                return Results.NotFound(new { message = "Topic not found" });
            }

            // Create notification payload
            var payload = JsonSerializer.Serialize(new
            {
                title = request.Title ?? "Notification",
                body = request.Body ?? "",
                icon = request.Icon ?? "",
                data = request.Data ?? ""
            });

            // Send to all push endpoints for this user
            var pushEndpoints = topic.User?.PushEndpoints ?? new List<PushEndpoint>();
            var results = new List<object>();

            foreach (var endpoint in pushEndpoints)
            {
                try
                {
                    await webPushService.SendNotificationAsync(endpoint.Endpoint, endpoint.P256dh, endpoint.Auth, payload);
                    results.Add(new { endpoint = endpoint.Name, status = "sent" });
                }
                catch (Exception ex)
                {
                    results.Add(new { endpoint = endpoint.Name, status = "failed", error = ex.Message });
                }
            }

            return Results.Ok(new { message = "Notifications sent", results });
        });

        // VAPID public key endpoint - intentionally public (no RequireAuth)
        // This must be accessible to unauthenticated users for push subscription setup
        app.MapGet("/vapid-public-key", (IConfiguration configuration) =>
        {
            var publicKey = configuration["VapidKeys:PublicKey"];
            
            if (string.IsNullOrEmpty(publicKey) || publicKey == "YOUR_VAPID_PUBLIC_KEY")
            {
                return Results.Ok(new { publicKey = (string?)null, configured = false });
            }
            
            return Results.Ok(new { publicKey, configured = true });
        });
    }
}
