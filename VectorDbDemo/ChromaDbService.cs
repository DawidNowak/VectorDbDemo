using ChromaDB.Client;
using Microsoft.Extensions.AI;

namespace VectorDbDemo;

public interface IChromaDbService
{
    Task<string> GetChromaVersion();
    Task SetupInitialVectors();
    Task AddVector(string collName, string text, Dictionary<string, object>? metadata = null);
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
        await AddVector(Consts.AnimalsCollection, "Wolf");
        await AddVector(Consts.AnimalsCollection, "Tiger");
        await AddVector(Consts.AnimalsCollection, "Eagle");
        await AddVector(Consts.AnimalsCollection, "Dolphin");
        await AddVector(Consts.AnimalsCollection, "Snake");
    }

    public async Task AddVector(string collName, string text, Dictionary<string, object>? metadata = null)
    {
        var vector = await _embeddings.GenerateEmbeddingVectorAsync(text);

        var coll = await _chromaClient.GetOrCreateCollection(collName);
        var collClient = new ChromaCollectionClient(coll, opts, _httpClient);

        await collClient.Add([text], [vector], metadata != null ? [metadata] : null);
    }

    public void Dispose() => _httpClient.Dispose();
}
