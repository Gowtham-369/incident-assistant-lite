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

public sealed class RetrievalOptions
{
    public const string Section = "Retrieval";
    public int ChunkCandidates { get; set; } = 20;
    public int VectorK { get; set; } = 50;
    public string VectorFilterMode { get; set; } = "preFilter";
    public double MinRerankerScore { get; set; } = 1.5;
    public int DefaultTop { get; set; } = 5;
}