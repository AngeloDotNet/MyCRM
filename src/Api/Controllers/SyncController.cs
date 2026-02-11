using Api.Data;
using Api.DTOs;
using Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/sync")]
public class SyncController(ApplicationDbContext db) : ControllerBase
{

    // Client sends lastSync timestamp and local changes; server returns serverChanges and conflicts
    [HttpPost]
    public async Task<IActionResult> SyncAsync([FromBody] SyncRequest request)
    {
        var serverChanges = new List<object>();
        var conflicts = new List<object>();

        // 1) Send server-side changes since last sync (contacts and companies)
        var contacts = await db.Contacts.Where(c => c.UpdatedAt > request.LastSync).AsNoTracking().ToListAsync();
        var companies = await db.Companies.Where(c => c.UpdatedAt > request.LastSync).AsNoTracking().ToListAsync();

        serverChanges.AddRange(contacts);
        serverChanges.AddRange(companies);

        // 2) Apply client changes (simple upsert); detect conflicts if server newer
        foreach (var c in request.Contacts ?? [])
        {
            var server = await db.Contacts.FindAsync(c.Id);

            if (server == null)
            {
                var newC = new Contact
                {
                    Id = c.Id == Guid.Empty ? Guid.NewGuid() : c.Id,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email,
                    Phone = c.Phone,
                    CompanyId = c.CompanyId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = c.UpdatedAt ?? DateTime.UtcNow
                };

                db.Contacts.Add(newC);
            }
            else
            {
                // conflict if server.UpdatedAt > client.UpdatedAt
                if (c.UpdatedAt.HasValue && server.UpdatedAt > c.UpdatedAt.Value)
                {
                    conflicts.Add(new ConflictContact("Contact", server.Id, server.UpdatedAt, c.UpdatedAt));
                    // policy: last-write-wins (server wins) — for now skip client's change
                }
                else
                {
                    server.FirstName = c.FirstName;
                    server.LastName = c.LastName;
                    server.Email = c.Email;
                    server.Phone = c.Phone;
                    server.CompanyId = c.CompanyId;
                    server.UpdatedAt = c.UpdatedAt ?? DateTime.UtcNow;
                }
            }
        }

        await db.SaveChangesAsync();

        var response = new SyncResponse(serverChanges, conflicts, DateTime.UtcNow);
        return Ok(response);
    }
}