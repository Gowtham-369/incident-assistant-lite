using IncidentAssistant.Api.Data;
using IncidentAssistant.Metadata.Enums;
using IncidentAssistant.Metadata.Responses;
using IncidentAssistant.Metadata.Requests;
using IncidentAssistant.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace IncidentAssistant.Api.Services;

public class IncidentService(AppDbContext db) : IIncidentService
{
    public async Task<IReadOnlyList<IncidentDto>> GetAllAsync(
        IncidentStatus? status, Severity? severity, CancellationToken ct)
    {
        // await Task.Delay(10_000, CancellationToken.None); // TEMP: 10s window to cancel
        var query = db.Incidents.AsNoTracking();
        if (status is not null) query = query.Where(i => i.Status == status);
        if (severity is not null) query = query.Where(i => i.Severity == severity);

        return await query
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => ToDto(i))
            .ToListAsync(ct);
    }

    public async Task<IncidentDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var incident = await db.Incidents.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, ct);
        return incident is null ? null : ToDto(incident);
    }

    public async Task<IncidentDto> CreateAsync(CreateIncidentRequest request, CancellationToken ct)
    {
        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            Severity = request.Severity,
            Category = request.Category,
            Status = IncidentStatus.Open,
            CreatedAt = DateTime.UtcNow
        };
        db.Incidents.Add(incident);
        await db.SaveChangesAsync(ct);
        return ToDto(incident);
    }

    public async Task<IncidentDto?> UpdateAsync(Guid id, UpdateIncidentRequest request, CancellationToken ct)
    {
        var incident = await db.Incidents.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (incident is null) return null;

        incident.Title = request.Title;
        incident.Description = request.Description;
        incident.Severity = request.Severity;
        incident.Category = request.Category;
        incident.ResolutionNotes = request.ResolutionNotes;

        if (request.Status == IncidentStatus.Resolved && incident.Status != IncidentStatus.Resolved)
            incident.ResolvedAt = DateTime.UtcNow;
        incident.Status = request.Status;

        await db.SaveChangesAsync(ct);
        return ToDto(incident);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var deleted = await db.Incidents.Where(i => i.Id == id).ExecuteDeleteAsync(ct);
        return deleted > 0;
    }

    private static IncidentDto ToDto(Incident i) =>
        new(i.Id, i.Title, i.Description, i.Severity, i.Status,
            i.Category, i.CreatedAt, i.ResolvedAt, i.ResolutionNotes);
}