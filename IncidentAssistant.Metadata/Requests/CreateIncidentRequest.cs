using IncidentAssistant.Metadata.Enums;
using System.ComponentModel.DataAnnotations;

namespace IncidentAssistant.Metadata.Requests;
public record CreateIncidentRequest(
    [Required, MaxLength(200)] string Title,
    [Required, MaxLength(4000)] string Description,
    Severity Severity,
    [MaxLength(100)] string? Category);