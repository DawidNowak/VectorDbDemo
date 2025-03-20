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

app.MapGet("/setup", async (IChromaDbService chromaDbService) =>
{
    await chromaDbService.SetupInitialVectors();
    return Results.Ok();
});

app.MapPost("/addvector", async (Product product, IChromaDbService chromaDbService) =>
{
    await chromaDbService.AddProductVector(Consts.ProductsCollection, product);
    return Results.Ok();
});

app.MapPost("/queryvector", async (StringObject body, IChromaDbService chromaDbService) =>
{
    var result = await chromaDbService.QueryVectors(Consts.ProductsCollection, body.Text);
    return Results.Json(result);
});

app.Run();

public record StringObject(string Text);