using Api.Data;
using Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/contacts")]
public class ContactsController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await db.Contacts.AsNoTracking().ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var item = await db.Contacts.FindAsync(id);

        if (item == null)
        {
            return NotFound();
        }

        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Contact model)
    {
        model.Id = Guid.NewGuid();
        model.CreatedAt = DateTime.UtcNow;
        model.UpdatedAt = DateTime.UtcNow;

        db.Contacts.Add(model);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = model.Id }, model);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Contact model)
    {
        var dbItem = await db.Contacts.FindAsync(id);

        if (dbItem == null)
        {
            return NotFound();
        }

        dbItem.FirstName = model.FirstName;
        dbItem.LastName = model.LastName;
        dbItem.Email = model.Email;
        dbItem.Phone = model.Phone;
        dbItem.CompanyId = model.CompanyId;
        dbItem.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var dbItem = await db.Contacts.FindAsync(id);

        if (dbItem == null)
        {
            return NotFound();
        }

        db.Contacts.Remove(dbItem);
        await db.SaveChangesAsync();

        return NoContent();
    }
}