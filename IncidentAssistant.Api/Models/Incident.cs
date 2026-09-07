namespace IncidentAssistant.Api.Models;

public enum Severity { Low, Medium, High, Critical }
public enum IncidentStatus { Open, InProgress, Resolved }

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
}