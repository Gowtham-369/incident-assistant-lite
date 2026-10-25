using System.Text;
using IncidentAssistant.Api.Models;

namespace IncidentAssistant.Api.Rag;

public static class IncidentChunker
{
    public static IReadOnlyList<IncidentChunk> Chunk(Incident incident, ChunkingStrategy strategy)
    {
        var symptoms = SymptomsBody(incident);
        var resolution = ResolutionBody(incident);

        if (strategy == ChunkingStrategy.WholeIncident)
        {
            var body = resolution is null ? symptoms : $"{symptoms}\n{resolution}";
            return [Create(incident, "full", body)];
        }

        var chunks = new List<IncidentChunk> { Create(incident, "symptoms", symptoms) };
        if (resolution is not null)
            chunks.Add(Create(incident, "resolution", resolution));
        return chunks;
    }

    private static string SymptomsBody(Incident i)
    {
        var sb = new StringBuilder(i.Description);
        if (i.ErrorSignatures.Count > 0)
            sb.Append("\nError signatures: ").Append(string.Join("; ", i.ErrorSignatures));
        return sb.ToString();
    }

    private static string? ResolutionBody(Incident i)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(i.RootCause)) parts.Add($"Root cause: {i.RootCause}");
        if (!string.IsNullOrWhiteSpace(i.ResolutionNotes)) parts.Add($"Resolution: {i.ResolutionNotes}");
        return parts.Count == 0 ? null : string.Join("\n", parts);
    }

    private static IncidentChunk Create(Incident i, string section, string body) => new()
    {
        ChunkId = $"{i.Id:N}_{section}",
        IncidentId = i.Id.ToString(),
        Title = i.Title,
        Content = $"{Header(i, section)}\n{body}",
        Section = section,
        Service = i.Service,
        Category = i.Category,
        Severity = i.Severity.ToString(),
        ErrorSignatures = [.. i.ErrorSignatures],
        AllowedGroups = [.. i.AllowedGroups],
        ResolvedAt = i.ResolvedAt is { } r
            ? new DateTimeOffset(DateTime.SpecifyKind(r, DateTimeKind.Utc))
            : null
    };

    private static string Header(Incident i, string section) =>
        $"Service: {i.Service} | Category: {i.Category ?? "Uncategorized"} | Severity: {i.Severity}\n" +
        $"Incident: {i.Title}\n" +
        $"Section: {section}";
}