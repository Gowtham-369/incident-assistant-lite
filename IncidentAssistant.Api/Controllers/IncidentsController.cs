using IncidentAssistant.Metadata.Enums;
using IncidentAssistant.Metadata.Responses;
using IncidentAssistant.Metadata.Requests;
using IncidentAssistant.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace IncidentAssistant.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class IncidentsController(IIncidentService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<IncidentDto>>> GetAll(
        [FromQuery] IncidentStatus? status, [FromQuery] Severity? severity, CancellationToken ct)
    {
        return Ok(await service.GetAllAsync(status, severity, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IncidentDto>> GetById(Guid id, CancellationToken ct)
        => await service.GetByIdAsync(id, ct) is { } dto ? Ok(dto) : NotFound();

    [HttpPost]
    public async Task<ActionResult<IncidentDto>> Create(CreateIncidentRequest request, CancellationToken ct)
    {
        var dto = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<IncidentDto>> Update(Guid id, UpdateIncidentRequest request, CancellationToken ct)
        => await service.UpdateAsync(id, request, ct) is { } dto ? Ok(dto) : NotFound();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => await service.DeleteAsync(id, ct) ? NoContent() : NotFound();
}