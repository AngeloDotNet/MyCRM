namespace Api.DTOs;

public record SyncRequest(DateTime LastSync, IEnumerable<ClientContact>? Contacts);