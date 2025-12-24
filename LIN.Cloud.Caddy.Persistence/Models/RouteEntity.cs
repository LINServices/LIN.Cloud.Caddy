using System.ComponentModel.DataAnnotations;

namespace LIN.Cloud.Caddy.Persistence.Models;

public class RouteEntity
{
    [Key]
    public string Id { get; set; } = string.Empty;

    [Required]
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}