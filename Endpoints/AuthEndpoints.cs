using WebPing.DTOs;
using WebPing.Services;

namespace WebPing.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", async (RegisterRequest request, IAuthService authService) =>
        {
            var user = await authService.RegisterAsync(request.Username, request.Password);
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
    }
}
