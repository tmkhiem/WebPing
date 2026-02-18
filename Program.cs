using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebPing.Data;
using WebPing.DTOs;
using WebPing.Middleware;
using WebPing.Models;
using WebPing.Services;
using WebPush;

// Check for command-line flags
if (args.Length > 0 && args[0] == "--generate-vapid-keys")
{
    GenerateVapidKeys();
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<WebPingDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=webping.db"));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWebPushService, WebPushService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<WebPingDbContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Serve static files (HTML, CSS, JS)
app.UseDefaultFiles(); // Serve index.html by default - must be before UseStaticFiles
app.UseStaticFiles();

// Add authentication middleware
app.UseMiddleware<BasicAuthMiddleware>();

// Auth endpoints
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

// Topic endpoints
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

// PushEndpoint endpoints
app.MapPost("/push-endpoints", async (RegisterPushEndpointRequest request, HttpContext context, WebPingDbContext dbContext) =>
{
    var username = context.Items["Username"]?.ToString();
    if (string.IsNullOrEmpty(username))
    {
        return Results.Unauthorized();
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

// Send notification endpoint
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

app.Run();

// Function to generate VAPID keys
static void GenerateVapidKeys()
{
    Console.WriteLine("=== VAPID Key Generation ===");
    Console.WriteLine();
    
    // Request email from user
    Console.Write("Enter your email address (e.g., mailto:admin@example.com): ");
    var email = Console.ReadLine()?.Trim() ?? "";
    
    // Validate email format
    if (string.IsNullOrEmpty(email))
    {
        Console.WriteLine("Error: Email address is required.");
        Environment.Exit(1);
    }
    
    // Add mailto: prefix if not present
    if (!email.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
    {
        email = $"mailto:{email}";
    }
    
    // Generate VAPID keys
    Console.WriteLine();
    Console.WriteLine("Generating VAPID keys...");
    var vapidKeys = VapidHelper.GenerateVapidKeys();
    
    // Create JSON structure
    var vapidConfig = new
    {
        VapidKeys = new
        {
            Subject = email,
            PublicKey = vapidKeys.PublicKey,
            PrivateKey = vapidKeys.PrivateKey
        }
    };
    
    var jsonOptions = new JsonSerializerOptions 
    { 
        WriteIndented = true 
    };
    var jsonOutput = JsonSerializer.Serialize(vapidConfig, jsonOptions);
    
    // Print the JSON
    Console.WriteLine();
    Console.WriteLine("Generated VAPID Keys:");
    Console.WriteLine("====================");
    Console.WriteLine(jsonOutput);
    Console.WriteLine();
    
    // Update appsettings.json
    var appsettingsPath = "appsettings.json";
    
    try
    {
        if (File.Exists(appsettingsPath))
        {
            // Read existing appsettings.json
            var existingJson = File.ReadAllText(appsettingsPath);
            var existingConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(existingJson);
            
            if (existingConfig != null)
            {
                // Update VapidKeys section
                existingConfig["VapidKeys"] = JsonSerializer.SerializeToElement(vapidConfig.VapidKeys);
                
                // Write back to file
                var updatedJson = JsonSerializer.Serialize(existingConfig, jsonOptions);
                File.WriteAllText(appsettingsPath, updatedJson);
                
                Console.WriteLine($"✓ VAPID keys saved to {appsettingsPath}");
            }
        }
        else
        {
            Console.WriteLine($"Warning: {appsettingsPath} not found. Keys not saved to file.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Failed to update {appsettingsPath}: {ex.Message}");
        Console.WriteLine("You can manually copy the JSON above to your appsettings.json file.");
    }
    
    Console.WriteLine();
    Console.WriteLine("Setup complete! You can now use these VAPID keys for push notifications.");
}
