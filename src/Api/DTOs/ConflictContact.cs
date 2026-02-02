namespace Api.DTOs;

public record class ConflictContact(string Entity, Guid Id, DateTime ServerUpdated, DateTime? ClientUpdated);