using IncidentAssistant.Metadata.Enums;
namespace IncidentAssistant.Metadata.Requests;
public record CreateIncidentRequest(
    string Title, string Description, Severity Severity, string? Category);