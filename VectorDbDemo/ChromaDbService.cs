using ChromaDB.Client;
using Microsoft.Extensions.AI;

namespace VectorDbDemo;

public interface IChromaDbService
{
    Task<string> GetChromaVersion();
    Task SetupInitialVectors();
    Task AddProductVector(string collName, Product product);
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
        await AddProductVector(Consts.ProductsCollection,
            [
                new Product(1, "Sunglasses",
                    "Stylish and durable sunglasses that protect your eyes from harmful UV rays. Perfect for sunny days, whether you're at the beach, driving, or hiking in the mountains."),
                new Product(2, "Running Shoes",
                    "High-performance running shoes designed for comfort and support. Ideal for long-distance runs, providing excellent cushioning and stability to enhance your running experience."),
                new Product(3, "Book: \"The Art of Coding\"",
                    "A comprehensive guide to mastering the art of coding. This book covers various programming languages and techniques, making it an essential read for both beginners and experienced developers."),
                new Product(4, "Indoor Plant: Fiddle Leaf Fig",
                    "A beautiful indoor plant that adds a touch of greenery to any space. The Fiddle Leaf Fig is known for its large, glossy leaves and is perfect for brightening up your home or office."),
                new Product(5, "Wireless Headphones",
                    "Premium wireless headphones with noise-canceling technology. Enjoy crystal-clear sound quality and comfort for hours, whether you're listening to music, taking calls, or working out.")
            ]
        );
    }

    public async Task AddProductVector(string collName, Product[] products)
    {
        var ids = products.Select(p => p.Id.ToString());
        var metadatas = products.Select(p => new Dictionary<string, object>() { [nameof(Product.Name)] = p.Name, [nameof(Product.Description)] = p.Description });

        var vectorTasks = products.Select(p => _embeddings.GenerateEmbeddingVectorAsync(p.Description));
        var vectors = await Task.WhenAll(vectorTasks);

        var coll = await _chromaClient.GetOrCreateCollection(collName);
        var collClient = new ChromaCollectionClient(coll, opts, _httpClient);
        await collClient.Add(
            [.. ids],
            [.. vectors],
            [.. metadatas]);
    }

    public async Task AddProductVector(string collName, Product product)
    {
        await AddProductVector(collName, [product]);
    }

    public async Task<List<string>> QueryVectors(string collName, string query, int topK = 2)
    {
        var queryVector = await _embeddings.GenerateEmbeddingVectorAsync(query);

        var coll = await _chromaClient.GetOrCreateCollection(collName);
        var collClient = new ChromaCollectionClient(coll, opts, _httpClient);

        var result = await collClient.Query(
            queryVector,
            topK,
            include: ChromaQueryInclude.Metadatas | ChromaQueryInclude.Distances);

        return [.. result.Select(r => $"Distance: {r.Distance} - {r.Metadata![nameof(Product.Name)]} - {r.Metadata![nameof(Product.Description)]}")];
    }

    public void Dispose() => _httpClient.Dispose();
}

public record Product(int Id, string Name, string Description);