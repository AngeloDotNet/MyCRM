using System.ComponentModel.DataAnnotations;

namespace Api.Entities;

public class Contact
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public string FirstName { get; set; }
    [Required] public string LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Guid? CompanyId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}