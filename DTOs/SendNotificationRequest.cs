namespace WebPing.DTOs;

public class SendNotificationRequest
{
    public string? Title { get; set; }
    public string? Body { get; set; }
    public string? Icon { get; set; }
    public string? Data { get; set; }
}
