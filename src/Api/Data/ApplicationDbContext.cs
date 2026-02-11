using Api.Entities;
using Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public virtual DbSet<Contact> Contacts { get; set; }
    public virtual DbSet<Company> Companies { get; set; }
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Contact>().ToTable("Contacts");
        builder.Entity<Company>().ToTable("Companies");
        builder.Entity<RefreshToken>().ToTable("RefreshTokens");
    }
}