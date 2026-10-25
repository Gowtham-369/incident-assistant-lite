using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace IncidentAssistant.Api.Rag;

public sealed class IncidentChunk
{
    [SimpleField(IsKey = true, IsFilterable = true)]
    public string ChunkId { get; set; } = "";

    [SimpleField(IsFilterable = true)]
    public string IncidentId { get; set; } = "";

    [SearchableField]
    public string Title { get; set; } = "";

    [SearchableField(AnalyzerName = LexicalAnalyzerName.Values.EnMicrosoft)]
    public string Content { get; set; } = "";

    [SimpleField(IsFilterable = true)]
    public string Section { get; set; } = "";

    [SimpleField(IsFilterable = true, IsFacetable = true)]
    public string Service { get; set; } = "";

    [SimpleField(IsFilterable = true)]
    public string? Category { get; set; }

    [SimpleField(IsFilterable = true)]
    public string Severity { get; set; } = "";

    [SearchableField(IsFilterable = true)]
    public string[] ErrorSignatures { get; set; } = [];

    [SimpleField(IsFilterable = true, IsHidden = true)]
    public string[] AllowedGroups { get; set; } = [];

    [SimpleField(IsFilterable = true, IsSortable = true)]
    public DateTimeOffset? ResolvedAt { get; set; }

    [SimpleField(IsFilterable = true)]
    public string EmbeddingModel { get; set; } = "";

    [VectorSearchField(VectorSearchDimensions = 1536,
                       VectorSearchProfileName = IncidentIndexDefinition.VectorProfile)]
    public float[] ContentVector { get; set; } = [];
}