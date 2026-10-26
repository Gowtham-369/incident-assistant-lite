using IncidentAssistant.Metadata.Responses;

namespace IncidentAssistant.Api.Services;
public interface IRagTriageService
    {
    Task<TriageResult> TriageAsync(IncidentDto incident, CancellationToken ct);
}