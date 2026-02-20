using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebPing.Data;
using WebPing.Endpoints;
using WebPing.Middleware;
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
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<WebPingDbContext>();
    context.Database.EnsureCreated();
    await MigrateDatabaseAsync(context);
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

// Map all endpoints
app.MapAuthEndpoints();
app.MapTopicEndpoints();
app.MapPushEndpointEndpoints();
app.MapNotificationEndpoints();

app.Run();

// Function to migrate database schema by adding missing columns
static async Task MigrateDatabaseAsync(WebPingDbContext context)
{
    var connection = context.Database.GetDbConnection();
    await connection.OpenAsync();

    try
    {
        // Check columns in Users table and add any missing ones
        var userColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "PRAGMA table_info(Users)";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                userColumns.Add(reader.GetString(1));
            }
        }

        if (userColumns.Count > 0 && !userColumns.Contains("Email"))
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE Users ADD COLUMN Email TEXT NOT NULL DEFAULT ''";
            await cmd.ExecuteNonQueryAsync();
        }
    }
    finally
    {
        await connection.CloseAsync();
    }
}

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
    
    // Basic email validation
    var emailToValidate = email.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) 
        ? email.Substring(7) 
        : email;
    
    if (!emailToValidate.Contains("@") || !emailToValidate.Contains(".") || emailToValidate.Length < 5)
    {
        Console.WriteLine("Error: Invalid email address format. Please provide a valid email (e.g., admin@example.com).");
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
            var existingConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(existingJson);
            
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
