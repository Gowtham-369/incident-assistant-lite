using IncidentAssistant.Api.Models;
using Microsoft.EntityFrameworkCore;
using IncidentAssistant.Metadata.Enums;

namespace IncidentAssistant.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Incident> Incidents => Set<Incident>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Incident>(e =>
        {
            e.Property(i => i.Title).HasMaxLength(200);
            e.Property(i => i.Severity).HasConversion<string>().HasMaxLength(20);
            e.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(i => i.Service).HasMaxLength(64).HasDefaultValue("unknown");
            e.Property(i => i.RootCause).HasMaxLength(2000);
            e.HasIndex(i => i.Service);
            e.HasData(
                new Incident { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Title = "API latency spike on checkout service",
                    Description = "p99 latency jumped from 200ms to 4s after the 14:00 deploy. Errors in logs mention connection pool exhaustion.",
                    Severity = Severity.High, Status = IncidentStatus.Open,
                    Category = "Performance", CreatedAt = new DateTime(2026, 9, 1, 14, 30, 0, DateTimeKind.Utc),
                    Service = "checkout-api" },
                new Incident { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Title = "Users unable to reset passwords",
                    Description = "Password reset emails not being delivered. SMTP relay returning 451 errors intermittently.",
                    Severity = Severity.Medium, Status = IncidentStatus.InProgress,
                    Category = "Email", CreatedAt = new DateTime(2026, 9, 3, 9, 15, 0, DateTimeKind.Utc),
                    Service = "notification-service" },
                new Incident { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Title = "Nightly ETL job failed",
                    Description = "Data warehouse sync failed at step 3 with a timeout. Downstream dashboards showing stale data.",
                    Severity = Severity.Low, Status = IncidentStatus.Resolved,
                    Category = "Data", CreatedAt = new DateTime(2026, 8, 28, 2, 0, 0, DateTimeKind.Utc),
                    ResolvedAt = new DateTime(2026, 8, 28, 8, 45, 0, DateTimeKind.Utc),
                    ResolutionNotes = "Increased timeout and added retry; root cause was a long-running lock from a manual query.",
                    Service = "data-pipeline" }
            );
        });
    }
}