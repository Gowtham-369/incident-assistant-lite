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

// TEMP smoke test — delete before any deployment to production. This is just to verify that the Azure OpenAI API is reachable and working.
app.MapGet("/api/_smoketest", (ResponsesClient client, IConfiguration config) =>
{
    var options = new CreateResponseOptions
    {
        Model = config["AzureOpenAI:DeploymentName"],
        InputItems = { ResponseItem.CreateUserMessageItem("Say 'ok' and nothing else.") }
    };
    var response = client.CreateResponse(options);
    return Results.Ok(response.Value.GetOutputText());
});

app.MapControllers();

app.Run();
