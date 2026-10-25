using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Options;

namespace IncidentAssistant.Api.Rag;

public sealed class IncidentRetriever(
    SearchIndexClient indexClient,
    EmbeddingService embeddings,
    IOptions<AzureSearchOptions> searchOptions,
    IOptions<RetrievalOptions> retrievalOptions,
    ILogger<IncidentRetriever> logger)
{
    private static readonly string[] SelectFields =
        ["chunkId", "incidentId", "title", "content", "service", "section"];

    public async Task<RetrievalResult> RetrieveAsync(RetrievalQuery q, CancellationToken ct)
    {
        var opts = retrievalOptions.Value;
        var indexName = q.Strategy == ChunkingStrategy.Sections
            ? searchOptions.Value.IndexName
            : searchOptions.Value.IndexNameWholeIncident;
        var client = indexClient.GetSearchClient(indexName);

        ReadOnlyMemory<float>? vector = q.Mode == RetrievalMode.Keyword
            ? null
            : (await embeddings.EmbedAsync([q.Text], ct))[0];

        var canFilterService = !string.IsNullOrWhiteSpace(q.Service) && q.Service != "unknown";

        var (mode, hits) = await SearchAsync(client, q, q.Mode, vector, canFilterService, ct);
        var incidents = Collapse(hits, q.Top);
        var fallback = false;

        if (canFilterService && !HasStrongEvidence(incidents, mode, opts))
        {
            (mode, hits) = await SearchAsync(client, q, mode, vector, serviceFilter: false, ct);
            incidents = Collapse(hits, q.Top);
            fallback = true;
        }

        logger.LogInformation(
            "Retrieval {Index} requested={Requested} executed={Executed} service={Service} " +
            "fallback={Fallback} chunkHits={Hits} incidents={Count} topReranker={Top}",
            indexName, q.Mode, mode, canFilterService ? q.Service : "(none)",
            fallback, hits.Count, incidents.Count, incidents.FirstOrDefault()?.RerankerScore);

        return new RetrievalResult(q.Mode, mode, indexName, fallback, hits.Count, incidents);
    }

    private async Task<(RetrievalMode Mode, List<SearchResult<IncidentChunk>> Hits)> SearchAsync(
        SearchClient client, RetrievalQuery q, RetrievalMode mode,
        ReadOnlyMemory<float>? vector, bool serviceFilter, CancellationToken ct)
    {
        try
        {
            return (mode, await RunAsync(client, q, mode, vector, serviceFilter, ct));
        }
        catch (RequestFailedException ex) when (mode == RetrievalMode.HybridSemantic && ex.Status == 400)
        {
            logger.LogWarning(ex, "Semantic ranking failed; retrying as plain hybrid");
            return (RetrievalMode.Hybrid, await RunAsync(client, q, RetrievalMode.Hybrid, vector, serviceFilter, ct));
        }
    }

    private async Task<List<SearchResult<IncidentChunk>>> RunAsync(
        SearchClient client, RetrievalQuery q, RetrievalMode mode,
        ReadOnlyMemory<float>? vector, bool serviceFilter, CancellationToken ct)
    {
        var opts = retrievalOptions.Value;
        var options = new SearchOptions
        {
            Size = opts.ChunkCandidates,
            Filter = BuildFilter(q, serviceFilter)
        };
        foreach (var field in SelectFields) options.Select.Add(field);

        if (mode != RetrievalMode.Keyword)
        {
            options.VectorSearch = new VectorSearchOptions
            {
                Queries =
                {
                    new VectorizedQuery(vector!.Value)
                    {
                        KNearestNeighborsCount = opts.VectorK,
                        Fields = { "contentVector" }
                    }
                },
                FilterMode = opts.VectorFilterMode == "postFilter"
                    ? VectorFilterMode.PostFilter
                    : VectorFilterMode.PreFilter
            };
        }

        if (mode == RetrievalMode.HybridSemantic)
        {
            options.QueryType = SearchQueryType.Semantic;
            options.SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = IncidentIndexDefinition.SemanticConfig
            };
        }

        // Vector mode sends no search text; every other mode sends sanitized text
        var searchText = mode == RetrievalMode.Vector ? null : QueryText.ForKeywordSearch(q.Text);

        var response = await client.SearchAsync<IncidentChunk>(searchText, options, ct);
        var hits = new List<SearchResult<IncidentChunk>>();
        await foreach (var hit in response.Value.GetResultsAsync()) hits.Add(hit);
        return hits;
    }

    private static string BuildFilter(RetrievalQuery q, bool serviceFilter)
    {
        var groups = string.Join(",", q.CallerGroups);
        var parts = new List<string>
        {
            SearchFilter.Create($"allowedGroups/any(g: search.in(g, {groups}, ','))")
        };
        if (serviceFilter)
            parts.Add(SearchFilter.Create($"service eq {q.Service}"));
        if (q.ExcludeIncidentId is { } self)
            parts.Add(SearchFilter.Create($"incidentId ne {self.ToString()}"));

        return string.Join(" and ", parts);
    }

    // Results arrive best first; GroupBy keeps first-seen order, so First() is each incident's best chunk
    private static List<RetrievedIncident> Collapse(List<SearchResult<IncidentChunk>> hits, int top) =>
        hits.GroupBy(h => h.Document.IncidentId)
            .Select(g => g.First())
            .Take(top)
            .Select(h => new RetrievedIncident(
                h.Document.IncidentId,
                h.Document.Title,
                h.Document.Service,
                h.Document.Section,
                h.Score ?? 0,
                h.SemanticSearch?.RerankerScore,
                h.Document.Content))
            .ToList();

    // Only the reranker score is comparable across queries, so only it can gate on quality.
    // For other modes, "strong" just means "found something".
    private static bool HasStrongEvidence(List<RetrievedIncident> incidents, RetrievalMode mode, RetrievalOptions opts) =>
        incidents.Count > 0 &&
        (mode != RetrievalMode.HybridSemantic || (incidents[0].RerankerScore ?? 0) >= opts.MinRerankerScore);
}