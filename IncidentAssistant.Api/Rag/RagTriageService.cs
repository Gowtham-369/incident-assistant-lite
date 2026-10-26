using System.ClientModel;
using System.Text.Json;
using IncidentAssistant.Api.Data;
using IncidentAssistant.Api.Services;
using IncidentAssistant.Metadata.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenAI.Responses;

namespace IncidentAssistant.Api.Rag;

public sealed class RagTriageService(
    IncidentRetriever retriever,
    AppDbContext db,
    ResponsesClient responses,
    IConfiguration config,
    IOptions<RetrievalOptions> retrievalOptions,
    ILogger<RagTriageService> logger) : IRagTriageService
{
    private enum Evidence { None, Weak, Unscored, Sufficient }
    private enum Level { Low, Medium, High }

    private sealed record ModelStep(string Text, List<string> Citations);
    private sealed record ModelOutput(
        string Summary, string? LikelyRootCause, string SuggestedSeverity,
        string SuggestedCategory, List<ModelStep> Steps, string Confidence);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<TriageResult> TriageAsync(IncidentDto incident, CancellationToken ct)
    {
        // 1. Retrieve
        var retrieval = await retriever.RetrieveAsync(new RetrievalQuery(
            Text: QueryText.FromIncident(incident),
            Service: incident.Service,
            ExcludeIncidentId: incident.Id,
            CallerGroups: ["oncall-all"], // Day 3B: from the caller's token
            Mode: RetrievalMode.HybridSemantic,
            Strategy: ChunkingStrategy.Sections,
            Top: retrievalOptions.Value.DefaultTop), ct);

        // 2. Load full source incidents from SQL, keep retrieval order, label S1..Sn
        var ids = retrieval.Incidents.Select(r => Guid.Parse(r.IncidentId)).ToList();
        var rows = await db.Incidents.AsNoTracking().Where(i => ids.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);

        var sources = retrieval.Incidents
            .Where(r => rows.ContainsKey(Guid.Parse(r.IncidentId)))
            .Select((r, n) => new TriageSource($"S{n + 1}", rows[Guid.Parse(r.IncidentId)], r.RerankerScore))
            .ToList();

        if (sources.Count < ids.Count)
            logger.LogWarning("{Missing} retrieved incidents no longer exist in SQL; the index is stale",
                ids.Count - sources.Count);

        // 3. Evidence gate: code decides whether the model is called at all
        var evidence = AssessEvidence(retrieval, sources);
        if (evidence == Evidence.None)
            return RetrievalOnly(incident, retrieval, sources, evidence, "no_evidence",
                "No similar past incidents were found.");
        if (evidence == Evidence.Weak)
            return RetrievalOnly(incident, retrieval, sources, evidence, "weak_evidence",
                "Similar past incidents were found, but none match closely enough to suggest steps.");

        // 4. Generate
        ModelOutput output;
        try
        {
            output = await GenerateAsync(incident, sources, ct);
        }
        catch (Exception ex) when (ex is ClientResultException or JsonException)
        {
            logger.LogError(ex, "Triage generation failed for {IncidentId}", incident.Id);
            return RetrievalOnly(incident, retrieval, sources, evidence, "generation_failed",
                "Suggestions are unavailable right now; showing similar past incidents.");
        }

        // 5. Validate citations and build the result
        return BuildResult(incident, retrieval, sources, evidence, output);
    }

    private Evidence AssessEvidence(RetrievalResult r, List<TriageSource> sources)
    {
        if (sources.Count == 0) return Evidence.None;
        if (r.ExecutedMode != RetrievalMode.HybridSemantic) return Evidence.Unscored;
        return (sources[0].RerankerScore ?? 0) >= retrievalOptions.Value.MinRerankerScore
            ? Evidence.Sufficient
            : Evidence.Weak;
    }

    private async Task<ModelOutput> GenerateAsync(IncidentDto incident, List<TriageSource> sources, CancellationToken ct)
    {
        var options = new CreateResponseOptions
        {
            Model = config["AzureOpenAI:DeploymentName"],
            Temperature = 0.1f,
            MaxOutputTokenCount = 800,
            InputItems =
            {
                ResponseItem.CreateSystemMessageItem(TriagePrompt.System),
                ResponseItem.CreateUserMessageItem(TriagePrompt.BuildUserMessage(incident, sources))
            },
            TextOptions = new ResponseTextOptions
            {
                TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
                    "IncidentTriage",
                    TriagePrompt.BuildSchema(sources.Select(s => s.Label).ToList()),
                    jsonSchemaIsStrict: true)
            }
        };

        var response = await responses.CreateResponseAsync(options, ct);
        return JsonSerializer.Deserialize<ModelOutput>(response.Value.GetOutputText(), Json)
               ?? throw new JsonException("Model returned empty output");
    }

    private TriageResult BuildResult(
        IncidentDto incident, RetrievalResult retrieval, List<TriageSource> sources,
        Evidence evidence, ModelOutput output)
    {
        var byLabel = sources.ToDictionary(s => s.Label);
        var invalidCitations = 0;
        var droppedSteps = 0;
        var steps = new List<TriageStepDto>();

        foreach (var step in output.Steps)
        {
            var valid = step.Citations.Where(byLabel.ContainsKey).Distinct().ToList();
            invalidCitations += step.Citations.Count(c => !byLabel.ContainsKey(c));

            if (valid.Count == 0 || string.IsNullOrWhiteSpace(step.Text))
            {
                droppedSteps++;
                continue;
            }
            steps.Add(new TriageStepDto(step.Text, valid.Select(l => byLabel[l].Incident.Id).ToList()));
        }

        if (steps.Count == 0)
        {
            var reason = output.Steps.Count == 0 ? "model_abstained" : "no_valid_steps";
            return RetrievalOnly(incident, retrieval, sources, evidence, reason,
                string.IsNullOrWhiteSpace(output.Summary) ? "No grounded steps could be suggested." : output.Summary,
                invalidCitations, droppedSteps);
        }

        var modelLevel = string.IsNullOrWhiteSpace(output.LikelyRootCause)
            ? Level.Low                      // no root cause identified: never more than low
            : Parse(output.Confidence);
        var confidence = Min(modelLevel, Cap(evidence, retrieval.FallbackToAllServices));
        var citedIds = steps.SelectMany(s => s.SourceIncidentIds).ToHashSet();

        logger.LogInformation(
            "Triage {IncidentId}: evidence={Evidence} steps={Steps} dropped={Dropped} invalidCitations={Invalid} " +
            "confidence={Confidence} sourcesCited={Cited}/{Provided}",
            incident.Id, evidence, steps.Count, droppedSteps, invalidCitations,
            confidence, citedIds.Count, sources.Count);

        return new TriageResult(
            SuggestedSeverity: output.SuggestedSeverity,
            SuggestedCategory: output.SuggestedCategory,
            TroubleshootingSteps: steps.Select(s => s.Text).ToList(),
            Summary: output.Summary,
            LikelyRootCause: output.LikelyRootCause,
            Steps: steps,
            Confidence: confidence.ToString().ToLowerInvariant(),
            Degraded: "none",
            Sources: sources.Where(s => citedIds.Contains(s.Incident.Id))
                .Select(s => new CitedIncidentDto(s.Incident.Id, s.Incident.Title, s.Incident.Service, s.RerankerScore))
                .ToList(),
            SimilarIncidents: ToSimilar(incident, retrieval),
            Diagnostics: Diagnostics(retrieval, evidence, sources, invalidCitations, droppedSteps));
    }

    private TriageResult RetrievalOnly(
        IncidentDto incident, RetrievalResult retrieval, List<TriageSource> sources,
        Evidence evidence, string degraded, string summary, int invalidCitations = 0, int droppedSteps = 0)
    {
        logger.LogInformation("Triage {IncidentId}: degraded={Degraded} evidence={Evidence} sources={Sources}",
            incident.Id, degraded, evidence, sources.Count);

        return new TriageResult(
            SuggestedSeverity: incident.Severity.ToString(),
            SuggestedCategory: incident.Category ?? "Uncategorized",
            TroubleshootingSteps: [],
            Summary: summary,
            LikelyRootCause: null,
            Steps: [],
            Confidence: "low",
            Degraded: degraded,
            Sources: [],
            SimilarIncidents: ToSimilar(incident, retrieval),
            Diagnostics: Diagnostics(retrieval, evidence, sources, invalidCitations, droppedSteps));
    }

    private static List<SimilarIncidentDto> ToSimilar(IncidentDto incident, RetrievalResult r) =>
        r.Incidents.Select(x => new SimilarIncidentDto(
            Guid.Parse(x.IncidentId), x.Title, x.Service, x.MatchedSection, x.Score, x.RerankerScore,
            x.Service != incident.Service && incident.Service != "unknown", x.MatchedContent)).ToList();

    private static TriageDiagnostics Diagnostics(
        RetrievalResult r, Evidence evidence, List<TriageSource> sources, int invalid, int dropped) =>
        new(r.ExecutedMode.ToString(), evidence.ToString(), sources.FirstOrDefault()?.RerankerScore,
            r.FallbackToAllServices, sources.Count, invalid, dropped);

    // Evidence caps how confident the answer may be, whatever the model says
    private static Level Cap(Evidence evidence, bool fallbackToAllServices) => evidence switch
    {
        Evidence.Sufficient when !fallbackToAllServices => Level.High,
        Evidence.Sufficient or Evidence.Unscored => Level.Medium,
        _ => Level.Low
    };

    private static Level Parse(string value) =>
        Enum.TryParse<Level>(value, ignoreCase: true, out var level) ? level : Level.Low;

    private static Level Min(Level a, Level b) => a < b ? a : b;
}