using Microsoft.EntityFrameworkCore;
using WebPing.Data;
using WebPing.DTOs;
using WebPing.Models;

namespace WebPing.Endpoints;

public static class TopicEndpoints
{
    public static void MapTopicEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/topics", async (CreateTopicRequest request, HttpContext context, WebPingDbContext dbContext) =>
        {
            var username = context.Items["Username"]?.ToString();
            if (string.IsNullOrEmpty(username))
            {
                return Results.Unauthorized();
            }

            // Check if topic already exists
            var existingTopic = await dbContext.Topics.FindAsync(request.Name);
            if (existingTopic != null)
            {
                return Results.BadRequest(new { message = "Topic already exists" });
            }

            var topic = new Topic
            {
                Name = request.Name,
                Username = username
            };

            dbContext.Topics.Add(topic);
            await dbContext.SaveChangesAsync();

            return Results.Ok(new { message = "Topic created successfully", topic = request.Name });
        });

        app.MapGet("/topics", async (HttpContext context, WebPingDbContext dbContext) =>
        {
            var username = context.Items["Username"]?.ToString();
            if (string.IsNullOrEmpty(username))
            {
                return Results.Unauthorized();
            }

            var topics = await dbContext.Topics
                .Where(t => t.Username == username)
                .Select(t => new { t.Name })
                .ToListAsync();

            return Results.Ok(topics);
        });
    }
}
