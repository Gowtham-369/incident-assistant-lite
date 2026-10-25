using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace IncidentAssistant.Api.Rag;

public static class IncidentIndexDefinition
{
    public const string VectorProfile = "incident-vector-profile";
    public const string HnswConfig = "incident-hnsw";
    public const string SemanticConfig = "incidents-semantic";

    public static SearchIndex Build(string indexName)
    {
        var fields = new FieldBuilder { Serializer = SearchSerialization.Serializer }
            .Build(typeof(IncidentChunk));
        
        // this validation prevents creating a bad schema that will fail at runtime when uploading vectors
        var vectorField = fields.Single(f => f.Name == "contentVector");
        if (vectorField.Type != SearchFieldDataType.Collection(SearchFieldDataType.Single)
            || vectorField.VectorSearchDimensions != 1536)
        {
            throw new InvalidOperationException(
                $"contentVector was built as {vectorField.Type} with " +
                $"{vectorField.VectorSearchDimensions?.ToString() ?? "no"} dimensions; " +
                "expected Collection(Edm.Single) with 1536.");
        }

        return new SearchIndex(indexName, fields)
        {
            VectorSearch = new VectorSearch
            {
                Algorithms =
                {
                    new HnswAlgorithmConfiguration(HnswConfig)
                    {
                        Parameters = new HnswParameters { Metric = VectorSearchAlgorithmMetric.Cosine }
                    }
                },
                Profiles = { new VectorSearchProfile(VectorProfile, HnswConfig) }
            },
            SemanticSearch = new SemanticSearch
            {
                Configurations =
                {
                    new SemanticConfiguration(SemanticConfig, new SemanticPrioritizedFields
                    {
                        TitleField = new SemanticField("title"),
                        ContentFields = { new SemanticField("content") },
                        KeywordsFields = { new SemanticField("errorSignatures"), new SemanticField("service") }
                    })
                }
            }
        };
    }
}