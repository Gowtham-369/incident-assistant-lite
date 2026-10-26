namespace IncidentAssistant.Metadata.Responses;

public record TriageResult(
    string SuggestedSeverity,
    string SuggestedCategory,
    List<string> TroubleshootingSteps,
    // Day 3A RAG additions
    string? Summary = null,
    string? LikelyRootCause = null,
    List<TriageStepDto>? Steps = null,
    string Confidence = "low",
    string Degraded = "none",
    List<CitedIncidentDto>? Sources = null,
    List<SimilarIncidentDto>? SimilarIncidents = null,
    TriageDiagnostics? Diagnostics = null);

public record TriageStepDto(string Text, List<Guid> SourceIncidentIds);

public record CitedIncidentDto(Guid IncidentId, string Title, string Service, double? RerankerScore);

public record TriageDiagnostics(
    string ExecutedMode,
    string Evidence,
    double? TopRerankerScore,
    bool FallbackToAllServices,
    int SourcesProvided,
    int InvalidCitations,
    int DroppedSteps);