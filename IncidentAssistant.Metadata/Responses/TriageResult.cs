namespace IncidentAssistant.Metadata.Responses;

public record TriageResult(
    string SuggestedSeverity,
    string SuggestedCategory,
    List<string> TroubleshootingSteps);