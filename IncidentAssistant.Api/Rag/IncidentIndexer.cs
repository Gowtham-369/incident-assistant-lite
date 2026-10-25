using Azure;
using Azure.Search.Documents.Indexes;
using IncidentAssistant.Api.Data;
using IncidentAssistant.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using IncidentAssistant.Metadata.Enums;

namespace IncidentAssistant.Api.Rag;

public sealed class IncidentIndexer(
    AppDbContext db,
    SearchIndexClient indexClient,
    EmbeddingService embeddings,
    IOptions<AzureSearchOptions> options,
    ILogger<IncidentIndexer> logger)
{
    public async Task RebuildAllAsync(CancellationToken ct)
    {
        var incidents = await db.Incidents.AsNoTracking()
            .Where(i => i.Status == IncidentStatus.Resolved)
            .ToListAsync(ct);

        logger.LogInformation("Reindex: {Count} resolved incidents loaded from SQL", incidents.Count);

        await RebuildAsync(incidents, ChunkingStrategy.WholeIncident, options.Value.IndexNameWholeIncident, ct);
        await RebuildAsync(incidents, ChunkingStrategy.Sections, options.Value.IndexName, ct);
    }

    private async Task RebuildAsync(
        List<Incident> incidents, ChunkingStrategy strategy, string indexName, CancellationToken ct)
    {
        try
        {
            await indexClient.DeleteIndexAsync(indexName, ct);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // first run: nothing to delete
        }

        await indexClient.CreateIndexAsync(IncidentIndexDefinition.Build(indexName), ct);

        var chunks = incidents.SelectMany(i => IncidentChunker.Chunk(i, strategy)).ToList();
        var vectors = await embeddings.EmbedAsync(chunks.Select(c => c.Content).ToList(), ct);

        for (var n = 0; n < chunks.Count; n++)
        {
            chunks[n].ContentVector = vectors[n].ToArray();
            chunks[n].EmbeddingModel = embeddings.ModelTag;
        }

        var searchClient = indexClient.GetSearchClient(indexName);
        var response = await searchClient.MergeOrUploadDocumentsAsync(chunks, cancellationToken: ct);
        var failed = response.Value.Results.Where(r => !r.Succeeded).ToList();

        foreach (var f in failed)
            logger.LogError("Upload failed for {Key}: {Error}", f.Key, f.ErrorMessage);

        logger.LogInformation(
            "Reindex {Index} ({Strategy}): {Uploaded} chunks uploaded, {Failed} failed",
            indexName, strategy, chunks.Count - failed.Count, failed.Count);
    }
}