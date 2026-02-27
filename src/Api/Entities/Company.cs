using System.ComponentModel.DataAnnotations;

namespace Api.Entities;

public class Company
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public string Name { get; set; }
    public string? Website { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
