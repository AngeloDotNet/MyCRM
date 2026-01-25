namespace Api.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public string TokenHash { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Revoked { get; set; } = false;
    public string? ReplacedByHash { get; set; }
}