namespace IncidentAssistant.Api.Rag;

public enum RetrievalMode { Keyword, Vector, Hybrid, HybridSemantic }

public sealed record RetrievalQuery(
    string Text,
    string? Service,
    Guid? ExcludeIncidentId,
    IReadOnlyList<string> CallerGroups,
    RetrievalMode Mode,
    ChunkingStrategy Strategy,
    int Top);

public sealed record RetrievedIncident(
    string IncidentId,
    string Title,
    string Service,
    string MatchedSection,
    double Score,
    double? RerankerScore,
    string MatchedContent);

public sealed record RetrievalResult(
    RetrievalMode RequestedMode,
    RetrievalMode ExecutedMode,
    string IndexName,
    bool FallbackToAllServices,
    int ChunkHits,
    IReadOnlyList<RetrievedIncident> Incidents);