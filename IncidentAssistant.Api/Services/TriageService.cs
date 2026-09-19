using System.Text.Json;
using IncidentAssistant.Metadata.Responses;
using OpenAI.Responses;

public class TriageService(ResponsesClient client, IConfiguration config) : ITriageService
{
    public async Task<TriageResult> TriageAsync(string title, string description, CancellationToken ct)
    {
        var prompt = $$"""
            You are an incident triage assistant. Given an incident title and
            description, respond with ONLY a JSON object, no markdown fences,
            no commentary, matching exactly this shape:
            {"suggestedSeverity":"Low|Medium|High|Critical","suggestedCategory":"string","troubleshootingSteps":["string","string"]}

            Title: {{title}}
            Description: {{description}}
            """;

        var options = new CreateResponseOptions
        {
            Model = config["AzureOpenAI:DeploymentName"],
            InputItems = { ResponseItem.CreateUserMessageItem(prompt) }
        };

        var response = await client.CreateResponseAsync(options, ct);
        var raw = response.Value.GetOutputText().Trim();

        // Defensive: strip markdown fences if the model added them anyway
        if (raw.StartsWith("```"))
            raw = raw.Trim('`').Replace("json", "", StringComparison.OrdinalIgnoreCase).Trim();

        try
        {
            var parsed = JsonSerializer.Deserialize<TriageResult>(raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return parsed ?? throw new InvalidOperationException("Empty triage result");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Model returned unparseable JSON: {raw}", ex);
        }
    }
}