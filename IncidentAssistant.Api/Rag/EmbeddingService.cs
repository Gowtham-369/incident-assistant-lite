using OpenAI.Embeddings;

namespace IncidentAssistant.Api.Rag;

public sealed class EmbeddingService(EmbeddingClient client, IConfiguration config)
{
    private const int BatchSize = 16;

    public string ModelTag => $"{config["AzureOpenAI:EmbeddingDeployment"]}@1536";

    public async Task<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> texts, CancellationToken ct)
    {
        var vectors = new List<ReadOnlyMemory<float>>(texts.Count);

        foreach (var batch in texts.Chunk(BatchSize))
        {
            var result = await client.GenerateEmbeddingsAsync(batch, cancellationToken: ct);
            vectors.AddRange(result.Value.OrderBy(e => e.Index).Select(e => e.ToFloats()));
        }

        return vectors;
    }
}