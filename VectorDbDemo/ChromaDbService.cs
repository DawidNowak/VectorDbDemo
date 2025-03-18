using ChromaDB.Client;

namespace VectorDbDemo;

public interface IChromaDbService
{
    Task<string> GetChromaVersion();
}

public class ChromaDbService : IChromaDbService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ChromaClient _chromaClient;

    public ChromaDbService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        var configOptions = new ChromaConfigurationOptions(uri: "http://localhost:8000/api/v1/");
        _chromaClient = new ChromaClient(configOptions, _httpClient);
    }

    public async Task<string> GetChromaVersion() => await _chromaClient.GetVersion();

    public void Dispose() => _httpClient.Dispose();
}
