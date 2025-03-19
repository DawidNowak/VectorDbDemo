using ChromaDB.Client;
using Microsoft.Extensions.AI;

namespace VectorDbDemo;

public interface IChromaDbService
{
    Task<string> GetChromaVersion();
    Task SetupInitialVectors();
    Task AddVector(string collName, string text, Dictionary<string, object>? metadata = null);
    Task<List<string>> QueryVectors(string collName, string text, int topK = 3);
}

public class ChromaDbService : IChromaDbService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ChromaClient _chromaClient;
    private readonly OllamaEmbeddingGenerator _embeddings;

    private static readonly ChromaConfigurationOptions opts = new(uri: "http://localhost:8000/api/v1/");

    public ChromaDbService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        _chromaClient = new ChromaClient(opts, _httpClient);
        _embeddings = new OllamaEmbeddingGenerator(new Uri("http://127.0.0.1:11434"), modelId: "all-minilm");
    }

    public async Task<string> GetChromaVersion() => await _chromaClient.GetVersion();

    public async Task SetupInitialVectors()
    {
        await AddVector(Consts.ProductsCollection, "Sunglasses - Stylish and durable sunglasses that protect your eyes from harmful UV rays. Perfect for sunny days, whether you're at the beach, driving, or hiking in the mountains.");
        await AddVector(Consts.ProductsCollection, "Running Shoes - High-performance running shoes designed for comfort and support. Ideal for long-distance runs, providing excellent cushioning and stability to enhance your running experience.");
        await AddVector(Consts.ProductsCollection, "Book: \"The Art of Coding\" - A comprehensive guide to mastering the art of coding. This book covers various programming languages and techniques, making it an essential read for both beginners and experienced developers.");
        await AddVector(Consts.ProductsCollection, "Indoor Plant: Fiddle Leaf Fig - A beautiful indoor plant that adds a touch of greenery to any space. The Fiddle Leaf Fig is known for its large, glossy leaves and is perfect for brightening up your home or office.");
        await AddVector(Consts.ProductsCollection, "Wireless Headphones - Premium wireless headphones with noise-canceling technology. Enjoy crystal-clear sound quality and comfort for hours, whether you're listening to music, taking calls, or working out.");
    }

    public async Task AddVector(string collName, string text, Dictionary<string, object>? metadata = null)
    {
        var vector = await _embeddings.GenerateEmbeddingVectorAsync(text);

        var coll = await _chromaClient.GetOrCreateCollection(collName);
        var collClient = new ChromaCollectionClient(coll, opts, _httpClient);

        await collClient.Add([text], [vector], metadata != null ? [metadata] : null);
    }

    public async Task<List<string>> QueryVectors(string collName, string text, int topK = 3)
    {
        var queryVector = await _embeddings.GenerateEmbeddingVectorAsync(text);

        var coll = await _chromaClient.GetOrCreateCollection(collName);
        var collClient = new ChromaCollectionClient(coll, opts, _httpClient);

        var result = await collClient.Query(queryVector, topK, include: ChromaQueryInclude.Distances);

        return [.. result.Select(r => $"Distance: {r.Distance} - {r.Id}")];
    }

    public void Dispose() => _httpClient.Dispose();
}
