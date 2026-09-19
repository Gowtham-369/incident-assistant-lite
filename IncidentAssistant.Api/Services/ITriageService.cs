using IncidentAssistant.Metadata.Responses;

public interface ITriageService
{
    Task<TriageResult> TriageAsync(string title, string description, CancellationToken ct);
}