namespace Api.DTOs;

public record SyncResponse(IEnumerable<object> ServerChanges, IEnumerable<object> Conflicts, DateTime ServerTime);