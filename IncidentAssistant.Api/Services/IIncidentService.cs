using IncidentAssistant.Metadata.Enums;
using IncidentAssistant.Metadata.Responses;
using IncidentAssistant.Metadata.Requests;

namespace IncidentAssistant.Api.Services;

public interface IIncidentService
{
    Task<IReadOnlyList<IncidentDto>> GetAllAsync(IncidentStatus? status, Severity? severity, CancellationToken ct);
    Task<IncidentDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IncidentDto> CreateAsync(CreateIncidentRequest request, CancellationToken ct);
    Task<IncidentDto?> UpdateAsync(Guid id, UpdateIncidentRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}