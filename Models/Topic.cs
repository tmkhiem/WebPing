namespace WebPing.Models;

public class Topic
{
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public User? User { get; set; }
}
