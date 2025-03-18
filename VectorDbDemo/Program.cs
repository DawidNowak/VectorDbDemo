using VectorDbDemo;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddScoped<IChromaDbService, ChromaDbService>();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/version", async (IChromaDbService chromaDbService) =>
{
    var version = await chromaDbService.GetChromaVersion();
    return Results.Text($"ChromaDB version: {version}");
});

app.Run();
