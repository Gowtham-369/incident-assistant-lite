using System.Text.Json;
using Azure.Core.Serialization;
using Azure.Search.Documents;

namespace IncidentAssistant.Api.Rag;

public sealed class AzureSearchOptions
{
    public const string Section = "AzureSearch";
    public string Endpoint { get; set; } = "";
    public string IndexName { get; set; } = "incidents-b";               // strategy B, the default
    public string IndexNameWholeIncident { get; set; } = "incidents-a";  // strategy A, for comparison
}

public enum ChunkingStrategy { WholeIncident, Sections }

public static class SearchSerialization
{
    // camelCase JSON so index field names are chunkId, content, contentVector...
    public static readonly JsonObjectSerializer Serializer =
        new(new JsonSerializerOptions(JsonSerializerDefaults.Web));

    public static SearchClientOptions CreateClientOptions() => new() { Serializer = Serializer };
}