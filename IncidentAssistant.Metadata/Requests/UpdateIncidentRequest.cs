using IncidentAssistant.Metadata.Enums;

namespace IncidentAssistant.Metadata.Requests;

public record UpdateIncidentRequest(
    string Title, string Description, Severity Severity,
    IncidentStatus Status, string? Category, string? ResolutionNotes);