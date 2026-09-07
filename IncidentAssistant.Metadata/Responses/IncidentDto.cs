using IncidentAssistant.Metadata.Enums;

namespace IncidentAssistant.Metadata.Responses;

public record IncidentDto(
    Guid Id, string Title, string Description, Severity Severity,
    IncidentStatus Status, string? Category, DateTime CreatedAt,
    DateTime? ResolvedAt, string? ResolutionNotes);