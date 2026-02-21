using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebPing.Constants;
using WebPing.Data;
using WebPing.DTOs;
using WebPing.Utilities;
using WebPing.Models;
using WebPing.Services;

namespace WebPing.Endpoints;

public static class NotificationEndpoints
{
    // Web push payload size limit is typically around 4KB, but we'll use a conservative limit
    // for the body text to account for JSON overhead and other fields
    private const int MaxBodyLength = 300;

    private static string TrimToWebPushLimit(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        if (text.Length <= MaxBodyLength)
            return text;

        // Trim and add ellipsis
        return text[..(MaxBodyLength - 3)] + "...";
    }

    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/send/{topicName}", async (string topicName, HttpContext context, WebPingDbContext dbContext, IWebPushService webPushService) =>
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

            // Read the request body
            string bodyContent;
            using (var reader = new StreamReader(context.Request.Body))
            {
                bodyContent = await reader.ReadToEndAsync();
            }

            string title;
            string body;
            string icon = "";
            string data = "";

            // Check if body is JSON or plain text
            if (!string.IsNullOrWhiteSpace(bodyContent) && bodyContent.TrimStart().StartsWith("{"))
            {
                // Try to parse as JSON (SendNotificationRequest)
                try
                {
                    var request = JsonSerializer.Deserialize<SendNotificationRequest>(bodyContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    title = request?.Title ?? topicName;
                    body = request?.Body ?? "";
                    icon = request?.Icon ?? "";
                    data = request?.Data ?? "";
                }
                catch
                {
                    // If JSON parsing fails, treat as plain text
                    title = topicName;
                    body = TrimToWebPushLimit(bodyContent);
                }
            }
            else
            {
                // Plain text body - use topic name as title
                title = topicName;
                body = TrimToWebPushLimit(bodyContent ?? "");
            }

            // Create notification payload
            var payload = JsonSerializer.Serialize(new
            {
                title,
                body,
                icon,
                data
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


        app.MapPost("/email/{topicName}", async (string topicName, HttpContext context, WebPingDbContext dbContext, IEmailService emailService) =>
        {
            // Find the topic
            var topic = await dbContext.Topics
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Name == topicName);

            if (topic == null)
            {
                return Results.NotFound(new { message = "Topic not found" });
            }

            // Read the request body
            string bodyContent;
            using (var reader = new StreamReader(context.Request.Body))
            {
                bodyContent = await reader.ReadToEndAsync();
            }

            string title;
            string body;

            // Check if body is JSON or plain text
            if (!string.IsNullOrWhiteSpace(bodyContent) && bodyContent.TrimStart().StartsWith("{"))
            {
                // Try to parse as JSON (SendNotificationRequest)
                try
                {
                    var request = JsonSerializer.Deserialize<SendNotificationRequest>(bodyContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    title = request?.Title ?? topicName;
                    body = request?.Body ?? "";
                }
                catch
                {
                    // If JSON parsing fails, treat as plain text
                    title = topicName;
                    body = bodyContent ?? "";
                }
            }
            else
            {
                // Plain text body - use topic name as title
                title = topicName;
                body = bodyContent ?? "";
            }

            var emailAddress = topic.User?.Email;
            if (string.IsNullOrEmpty(emailAddress))
            {
                return Results.BadRequest(new { message = "User does not have an email address configured" });
            }
            try
            {
                await emailService.SendEmailAsync(emailAddress, title, body);
                return Results.Ok(new { message = "Email sent successfully" });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500, title: "Failed to send email");
            }

        });


        // VAPID public key endpoint - requires authentication
        // Users must be logged in to register browsers for push notifications
        app.MapGet("/vapid-public-key", (IConfiguration configuration) =>
        {
            var publicKey = configuration["VapidKeys:PublicKey"];

            if (string.IsNullOrEmpty(publicKey) || publicKey == VapidConstants.PlaceholderPublicKey)
            {
                return Results.Ok(new { publicKey = (string?)null, configured = false });
            }

            return Results.Ok(new { publicKey, configured = true });
        }).RequireAuth();
    }
}
