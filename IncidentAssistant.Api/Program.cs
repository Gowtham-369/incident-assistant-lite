using System.Text.Json.Serialization;
using IncidentAssistant.Api.Data;
using IncidentAssistant.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Scalar.AspNetCore;
using OpenAI.Responses;
using System.ClientModel;
using OpenAI;
using Azure.Core;
using Azure.Identity;
using IncidentAssistant.Api.Rag;
using Microsoft.Extensions.Options;
using Azure.Search.Documents.Indexes;
using System.ClientModel.Primitives;
using OpenAI.Embeddings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IIncidentService, IncidentService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var endpoint = config["AzureOpenAI:Endpoint"]!;
    var deployment = config["AzureOpenAI:DeploymentName"]!;
    var apiKey = config["AzureOpenAI:ApiKey"]!;

    return new ResponsesClient(
        credential: new ApiKeyCredential(apiKey),
        options: new ResponsesClientOptions { Endpoint = new Uri(endpoint) });
});

builder.Services.AddScoped<ITriageService, TriageService>();

// One credential for every Azure call: az login locally, managed identity on App Service
builder.Services.AddSingleton<TokenCredential>(new DefaultAzureCredential());

builder.Services.AddOptions<AzureSearchOptions>()
    .Bind(builder.Configuration.GetSection(AzureSearchOptions.Section))
    .Validate(o => !string.IsNullOrWhiteSpace(o.Endpoint), "AzureSearch:Endpoint is required")
    .ValidateOnStart();

builder.Services.AddSingleton(sp =>
{
    var o = sp.GetRequiredService<IOptions<AzureSearchOptions>>().Value;
    return new SearchIndexClient(
        new Uri(o.Endpoint),
        sp.GetRequiredService<TokenCredential>(),
        SearchSerialization.CreateClientOptions());
});

builder.Services.AddSingleton(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var policy = new BearerTokenPolicy(
        sp.GetRequiredService<TokenCredential>(), "https://ai.azure.com/.default");

    return new EmbeddingClient(
        cfg["AzureOpenAI:EmbeddingDeployment"]!,
        policy,
        new OpenAIClientOptions { Endpoint = new Uri(cfg["AzureOpenAI:Endpoint"]!) });
});

builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddScoped<IncidentIndexer>();

builder.Services.AddOptions<RetrievalOptions>()
    .Bind(builder.Configuration.GetSection(RetrievalOptions.Section));

builder.Services.AddSingleton<IncidentRetriever>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// TEMP smoke test — delete/disable before deploying to production. This is just to verify that the Azure OpenAI API is reachable and working.
/*
if(app.Environment.IsDevelopment())
{
    app.MapGet("/api/_smoketest", async (ResponsesClient client, IConfiguration config) =>
    {
        var options = new CreateResponseOptions
        {
            Model = config["AzureOpenAI:DeploymentName"],
            InputItems = { ResponseItem.CreateUserMessageItem("Say 'ok' and nothing else.") }
        };
        var response = await client.CreateResponseAsync(options);
        return Results.Ok(response.Value.GetOutputText());
    });
}
*/

app.MapControllers();

if(app.Configuration.GetValue<bool>("Seeding:Enabled"))
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var path = Path.Combine(app.Environment.ContentRootPath, "Seed", "incidents.json");
        await IncidentSeeder.SeedAsync(db, path, logger);
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Incident seeding failed; the API will start without it");
    }
    
}

// Admin action: rebuild both indexes and exit. Never exposed over HTTP.
if (app.Configuration.GetValue<bool>("Reindex"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<IncidentIndexer>().RebuildAllAsync(CancellationToken.None);
    return;
}

app.Run();
