using IncidentAssistant.Metadata.Enums;

namespace IncidentAssistant.Metadata.Requests;

public record UpdateIncidentRequest(
    string Title, string Description, Severity Severity,
    IncidentStatus Status, string? Category, string? ResolutionNotes,
    string? Service = null, string? RootCause = null,
    IReadOnlyList<string>? ErrorSignatures = null);