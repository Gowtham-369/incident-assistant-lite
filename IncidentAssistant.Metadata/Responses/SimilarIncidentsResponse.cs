namespace IncidentAssistant.Metadata.Responses;

public record SimilarIncidentsResponse(
    Guid ForIncidentId, string RequestedMode, string ExecutedMode, string Index,
    bool FallbackToAllServices, int ChunkHits, IReadOnlyList<SimilarIncidentDto> Results);