using IncidentAssistant.Metadata.Enums;

namespace IncidentAssistant.Api.Models;


public class Incident
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public Severity Severity { get; set; }
    public IncidentStatus Status { get; set; } = IncidentStatus.Open;
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public string Service { get; set; } = "unknown";
    public string? RootCause { get; set; }
    public List<string> ErrorSignatures { get; set; } = [];
    public List<string> AllowedGroups { get; set; } = ["oncall-all"];
}