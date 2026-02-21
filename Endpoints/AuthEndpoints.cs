using WebPing.DTOs;
using WebPing.Utilities;
using WebPing.Services;

namespace WebPing.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", async (RegisterRequest request, IAuthService authService) =>
        {
            if (string.IsNullOrEmpty(request.Password) || request.Password.Length < 6)
            {
                return Results.BadRequest(new { message = "Password must be at least 6 characters" });
            }

            var user = await authService.RegisterAsync(request.Username, request.Password, request.Email);
            if (user == null)
            {
                return Results.BadRequest(new { message = "User already exists" });
            }
            return Results.Ok(new { message = "User registered successfully", username = user.Username });
        });

        app.MapPost("/auth/login", async (LoginRequest request, IAuthService authService) =>
        {
            var user = await authService.LoginAsync(request.Username, request.Password);
            if (user == null)
            {
                return Results.Unauthorized();
            }
            return Results.Ok(new { message = "Login successful", username = user.Username });
        });

        app.MapGet("/auth/profile", async (HttpContext httpContext, IAuthService authService) =>
        {
            var username = httpContext.Items["Username"] as string;
            if (string.IsNullOrEmpty(username))
            {
                return Results.Unauthorized();
            }

            var user = await authService.GetUserAsync(username);
            if (user == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new { username = user.Username, email = user.Email });
        }).RequireAuth();

        app.MapGet("/auth/email", async (HttpContext httpContext, IAuthService authService) =>
        {
            var username = httpContext.Items["Username"] as string;
            if (string.IsNullOrEmpty(username))
            {
                return Results.Unauthorized();
            }

            var user = await authService.GetUserAsync(username);
            if (user == null)
            {
                return Results.NotFound();
            }

            if (string.IsNullOrEmpty(user.Email))
            {
                return Results.BadRequest(new { message = "No email registered. Use PUT /auth/email to set your email address." });
            }

            return Results.Ok(new { email = user.Email });
        }).RequireAuth();

        app.MapPut("/auth/email", async (UpdateEmailRequest request, HttpContext httpContext, IAuthService authService) =>
        {
            var username = httpContext.Items["Username"] as string;
            if (string.IsNullOrEmpty(username))
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrEmpty(request.Email) || !request.Email.Contains('@'))
            {
                return Results.BadRequest(new { message = "A valid email address is required" });
            }

            var success = await authService.UpdateEmailAsync(username, request.Email);
            if (!success)
            {
                return Results.NotFound();
            }

            return Results.Ok(new { message = "Email updated successfully" });
        }).RequireAuth();

        app.MapPost("/auth/change-password", async (ChangePasswordRequest request, HttpContext httpContext, IAuthService authService) =>
        {
            var username = httpContext.Items["Username"] as string;
            if (string.IsNullOrEmpty(username))
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 6)
            {
                return Results.BadRequest(new { message = "Password must be at least 6 characters" });
            }

            var success = await authService.ChangePasswordAsync(username, request.NewPassword);
            if (!success)
            {
                return Results.NotFound();
            }

            return Results.Ok(new { message = "Password changed successfully" });
        }).RequireAuth();
    }
}
