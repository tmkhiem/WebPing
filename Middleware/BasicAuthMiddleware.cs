using System.Text;
using WebPing.Utilities;
using WebPing.Services;

namespace WebPing.Middleware;

public class BasicAuthMiddleware
{
    private readonly RequestDelegate _next;

    public BasicAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuthService authService)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        
        // Skip auth for static files from wwwroot
        if (IsStaticFile(path))
        {
            await _next(context);
            return;
        }

        // Check if the endpoint requires authentication via metadata
        var endpoint = context.GetEndpoint();
        var requiresAuth = endpoint?.Metadata.GetMetadata<RequireAuthAttribute>() != null;

        // If endpoint doesn't require auth, skip authentication
        if (!requiresAuth)
        {
            await _next(context);
            return;
        }

        // Check for Authorization header
        if (!context.Request.Headers.ContainsKey("Authorization"))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        var authHeader = context.Request.Headers["Authorization"].ToString();
        if (!authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        try
        {
            var encodedCredentials = authHeader.Substring("Basic ".Length).Trim();
            var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
            var parts = credentials.Split(':', 2);

            if (parts.Length != 2)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }

            var username = parts[0];
            var password = parts[1];

            var user = await authService.LoginAsync(username, password);
            if (user == null)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }

            // Store username in HttpContext items for later use
            context.Items["Username"] = username;

            await _next(context);
        }
        catch (FormatException)
        {
            // Invalid Base64 string
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized");
        }
    }

    private static bool IsStaticFile(string path)
    {
        // Only allow static files with common extensions or root path
        if (path == "/") return true;
        
        var staticExtensions = new[] { ".html", ".css", ".js", ".ico", ".png", ".jpg", ".jpeg", ".svg", ".gif", ".woff", ".woff2", ".ttf", ".eot" };
        return staticExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }
}
