using System.ComponentModel.DataAnnotations;

namespace WebPing.DTOs;

public class UpdatePushEndpointRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
}
