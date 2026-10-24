using System.Text.Json;
using System.Text.Json.Serialization;
using IncidentAssistant.Api.Models;
using Microsoft.EntityFrameworkCore;
using IncidentAssistant.Metadata.Enums;

namespace IncidentAssistant.Api.Data;

public sealed record SeedIncident(
    Guid Id, string Title, string Description, Severity Severity,
    string? Category, string Service, string? RootCause, string? ResolutionNotes,
    List<string> ErrorSignatures, List<string>? AllowedGroups,
    DateTime CreatedAt, DateTime ResolvedAt);

public static class IncidentSeeder
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public static async Task SeedAsync(AppDbContext db, string path, ILogger logger, CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(path);
        var seeds = await JsonSerializer.DeserializeAsync<List<SeedIncident>>(stream, JsonOptions, ct) ?? [];

        var existing = (await db.Incidents.Select(i => i.Id).ToListAsync(ct)).ToHashSet();

        var toAdd = seeds
            .Where(s => !existing.Contains(s.Id))
            .Select(s => new Incident
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                Severity = s.Severity,
                Status = IncidentStatus.Resolved,
                Category = s.Category,
                Service = s.Service,
                RootCause = s.RootCause,
                ResolutionNotes = s.ResolutionNotes,
                ErrorSignatures = s.ErrorSignatures,
                AllowedGroups = s.AllowedGroups ?? ["oncall-all"],
                CreatedAt = DateTime.SpecifyKind(s.CreatedAt, DateTimeKind.Utc),
                ResolvedAt = DateTime.SpecifyKind(s.ResolvedAt, DateTimeKind.Utc)
            })
            .ToList();

        db.Incidents.AddRange(toAdd);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Incident seeding: added {Added}, skipped {Skipped} already present",
            toAdd.Count, seeds.Count - toAdd.Count);
    }
}