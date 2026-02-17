namespace WebPing.Models;

public class PushEndpoint
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public User? User { get; set; }
}
