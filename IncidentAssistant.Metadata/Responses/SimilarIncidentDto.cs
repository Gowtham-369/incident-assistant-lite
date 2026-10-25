namespace IncidentAssistant.Metadata.Responses;
public record SimilarIncidentDto(
    Guid IncidentId, string Title, string Service, string MatchedSection,
    double Score, double? RerankerScore, bool CrossService, string MatchedContent);