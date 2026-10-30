using IncidentAssistant.Metadata.Enums;
using IncidentAssistant.Metadata.Responses;
using IncidentAssistant.Metadata.Requests;
using IncidentAssistant.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IncidentAssistant.Api.Rag;
using Microsoft.Extensions.Options;

namespace IncidentAssistant.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class IncidentsController(IIncidentService service, ITriageService triageService, IRagTriageService ragTriageService) : ControllerBase
{
    [HttpGet]
    [EndpointName("ListIncidents")]
    [EndpointSummary("List incidents")]
    public async Task<ActionResult<IReadOnlyList<IncidentDto>>> GetAll(
        [FromQuery] IncidentStatus? status, [FromQuery] Severity? severity, CancellationToken ct)
    {
        return Ok(await service.GetAllAsync(status, severity, ct));
    }

    [HttpGet("{id:guid}")]
    [EndpointName("GetIncidentById")]
    [EndpointSummary("Get incident by ID")]
    public async Task<ActionResult<IncidentDto>> GetById(Guid id, CancellationToken ct)
        => await service.GetByIdAsync(id, ct) is { } dto ? Ok(dto) : NotFound();

    [HttpPost]
    [EndpointName("CreateIncident")]
    [EndpointSummary("Create an incident")]
    public async Task<ActionResult<IncidentDto>> Create(CreateIncidentRequest request, CancellationToken ct)
    {
        var dto = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [EndpointName("UpdateIncident")]
    [EndpointSummary("Update an incident")]
    public async Task<ActionResult<IncidentDto>> Update(Guid id, UpdateIncidentRequest request, CancellationToken ct)
        => await service.UpdateAsync(id, request, ct) is { } dto ? Ok(dto) : NotFound();

    [HttpDelete("{id:guid}")]
    [EndpointName("DeleteIncident")]
    [EndpointSummary("Delete an incident")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => await service.DeleteAsync(id, ct) ? NoContent() : NotFound();

    [HttpPost("{id:guid}/triage")]
    [EndpointName("TriageIncident")]
    [EndpointSummary("Triage an incident using past incidents (RAG)")]
    public async Task<ActionResult<TriageResult>> Triage(Guid id, CancellationToken ct)
    {
        var incident = await service.GetByIdAsync(id, ct);
        if (incident is null) return NotFound();

        var result = await ragTriageService.TriageAsync(incident, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/triage/baseline")]
    [EndpointName("TriageIncidentBaseline")]
    [EndpointSummary("Triage an incident using a baseline approach without retrieval")]
    public async Task<ActionResult<TriageResult>> TriageBaseline(Guid id, CancellationToken ct)
    {
        var incident = await service.GetByIdAsync(id, ct);
        if (incident is null) return NotFound();

        var result = await triageService.TriageAsync(incident.Title, incident.Description, ct);
        return Ok(result with { Degraded = "baseline_no_retrieval", Confidence = "unassessed" });
    }

    [HttpGet("{id:guid}/similar")]
    [EndpointName("GetSimilarIncidents")]
    [EndpointSummary("Get similar incidents")]
    public async Task<ActionResult<SimilarIncidentsResponse>> GetSimilar(
        Guid id,
        [FromServices] IncidentRetriever retriever,
        [FromServices] IOptions<RetrievalOptions> retrievalOptions,
        [FromQuery] RetrievalMode mode = RetrievalMode.HybridSemantic,
        [FromQuery] ChunkingStrategy strategy = ChunkingStrategy.Sections,
        [FromQuery] int? top = null,
        CancellationToken ct = default)
    {
        var incident = await service.GetByIdAsync(id, ct); // use your IIncidentService field name
        if (incident is null) return NotFound();

        var result = await retriever.RetrieveAsync(new RetrievalQuery(
            Text: QueryText.FromIncident(incident),
            Service: incident.Service,
            ExcludeIncidentId: incident.Id,
            CallerGroups: ["oncall-all"], // Day 3B: read from the caller's token
            Mode: mode,
            Strategy: strategy,
            Top: Math.Clamp(top ?? retrievalOptions.Value.DefaultTop, 1, 10)), ct);

        return Ok(new SimilarIncidentsResponse(
            incident.Id, result.RequestedMode.ToString(), result.ExecutedMode.ToString(),
            result.IndexName, result.FallbackToAllServices, result.ChunkHits,
            result.Incidents.Select(r => new SimilarIncidentDto(
                Guid.Parse(r.IncidentId), r.Title, r.Service, r.MatchedSection,
                r.Score, r.RerankerScore, r.Service != incident.Service && r.Service != incident.Service && incident.Service != "unknown", r.MatchedContent)).ToList()));
    }
}