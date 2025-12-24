using System.ComponentModel.DataAnnotations;

namespace LIN.Cloud.Caddy.Persistence.Models;

public class ApiKeyEntity
{
    [Key]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}