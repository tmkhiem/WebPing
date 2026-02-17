using Microsoft.EntityFrameworkCore;
using WebPing.Data;
using WebPing.Models;

namespace WebPing.Services;

public interface IAuthService
{
    Task<User?> RegisterAsync(string username, string password);
    Task<User?> LoginAsync(string username, string password);
    Task<User?> GetUserAsync(string username);
}

public class AuthService : IAuthService
{
    private readonly WebPingDbContext _context;

    public AuthService(WebPingDbContext context)
    {
        _context = context;
    }

    public async Task<User?> RegisterAsync(string username, string password)
    {
        // Check if user already exists
        var existingUser = await _context.Users.FindAsync(username);
        if (existingUser != null)
        {
            return null;
        }

        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        var user = await _context.Users.FindAsync(username);
        if (user == null)
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return null;
        }

        return user;
    }

    public async Task<User?> GetUserAsync(string username)
    {
        return await _context.Users
            .Include(u => u.Topics)
            .Include(u => u.PushEndpoints)
            .FirstOrDefaultAsync(u => u.Username == username);
    }
}
