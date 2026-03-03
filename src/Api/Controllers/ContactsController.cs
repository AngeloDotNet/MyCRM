using Api.Data;
using Api.DTOs;
using Api.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/contacts")]
public class ContactsController(ApplicationDbContext db, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await db.Contacts.AsNoTracking().ToListAsync();
        var dto = mapper.Map<IEnumerable<ContactDto>>(items);

        return Ok(dto);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var item = await db.Contacts.FindAsync(id);

        if (item == null)
        {
            return NotFound();
        }

        return Ok(mapper.Map<ContactDto>(item));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ContactDto model)
    {
        var entity = mapper.Map<Contact>(model);

        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        db.Contacts.Add(entity);
        await db.SaveChangesAsync();

        var dto = mapper.Map<ContactDto>(entity);
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Contact model)
    {
        var dbItem = await db.Contacts.FindAsync(id);

        if (dbItem == null)
        {
            return NotFound();
        }

        mapper.Map(model, dbItem);
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