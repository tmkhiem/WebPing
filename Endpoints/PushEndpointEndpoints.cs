using Microsoft.EntityFrameworkCore;
using WebPing.Data;
using WebPing.DTOs;
using WebPing.Models;

namespace WebPing.Endpoints;

public static class PushEndpointEndpoints
{
    public static void MapPushEndpointEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/push-endpoints", async (RegisterPushEndpointRequest request, HttpContext context, WebPingDbContext dbContext) =>
        {
            var username = context.Items["Username"]?.ToString();
            if (string.IsNullOrEmpty(username))
            {
                return Results.Unauthorized();
            }

            // Check if this endpoint is already registered for this user
            var existingEndpoint = await dbContext.PushEndpoints
                .FirstOrDefaultAsync(p => p.Endpoint == request.Endpoint && p.Username == username);
            
            if (existingEndpoint != null)
            {
                return Results.BadRequest(new { message = "This browser is already registered" });
            }

            var endpoint = new PushEndpoint
            {
                Name = request.Name,
                Endpoint = request.Endpoint,
                P256dh = request.P256dh,
                Auth = request.Auth,
                Username = username
            };

            dbContext.PushEndpoints.Add(endpoint);
            await dbContext.SaveChangesAsync();

            return Results.Ok(new { message = "Push endpoint registered successfully", id = endpoint.Id });
        });

        app.MapGet("/push-endpoints", async (HttpContext context, WebPingDbContext dbContext) =>
        {
            var username = context.Items["Username"]?.ToString();
            if (string.IsNullOrEmpty(username))
            {
                return Results.Unauthorized();
            }

            var endpoints = await dbContext.PushEndpoints
                .Where(p => p.Username == username)
                .Select(p => new { p.Id, p.Name, p.Endpoint })
                .ToListAsync();

            return Results.Ok(endpoints);
        });

        app.MapDelete("/push-endpoints/{id}", async (int id, HttpContext context, WebPingDbContext dbContext) =>
        {
            var username = context.Items["Username"]?.ToString();
            if (string.IsNullOrEmpty(username))
            {
                return Results.Unauthorized();
            }

            var endpoint = await dbContext.PushEndpoints
                .FirstOrDefaultAsync(p => p.Id == id && p.Username == username);

            if (endpoint == null)
            {
                return Results.NotFound(new { message = "Push endpoint not found" });
            }

            dbContext.PushEndpoints.Remove(endpoint);
            await dbContext.SaveChangesAsync();

            return Results.Ok(new { message = "Push endpoint deleted successfully" });
        });
    }
}
