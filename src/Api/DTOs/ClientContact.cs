namespace Api.DTOs;

public record ClientContact(Guid Id, string FirstName, string LastName, string? Email, string? Phone, Guid? CompanyId, DateTime? UpdatedAt);