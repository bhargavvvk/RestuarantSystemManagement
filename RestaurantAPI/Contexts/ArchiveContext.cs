using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Models;

namespace RestaurantAPI.Contexts;

public class ArchiveContext : DbContext
{
    public ArchiveContext(DbContextOptions<ArchiveContext> options)
        : base(options)
    {
    }

    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(audit =>
        {
            audit.HasKey(a => a.Id)
                .HasName("PK_AuditLog");

            audit.Property(a => a.Action)
                .HasConversion<string>();

            audit.Property(a => a.OldValues)
                .HasColumnType("jsonb");

            audit.Property(a => a.NewValues)
                .HasColumnType("jsonb");

            audit.Property(a => a.PerformedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
        });
    }
}