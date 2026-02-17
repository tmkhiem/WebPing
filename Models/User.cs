namespace WebPing.Models;

public class User
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public ICollection<Topic> Topics { get; set; } = new List<Topic>();
    public ICollection<PushEndpoint> PushEndpoints { get; set; } = new List<PushEndpoint>();
}
