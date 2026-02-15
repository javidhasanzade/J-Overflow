using System.Text.RegularExpressions;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SearchService.Data;
using SearchService.Models;
using Typesense;
using Typesense.Setup;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.AddServiceDefaults();

var typesenseUri = builder.Configuration["services:typesense:typesense:0"];
if (string.IsNullOrEmpty(typesenseUri))
    throw new InvalidOperationException("typesense uri is not set");

var typesenseApiKey = builder.Configuration["typesense-api-key"];
if (string.IsNullOrEmpty(typesenseApiKey))
    throw new InvalidOperationException("typesense api key is not set");

var uri = new Uri(typesenseUri);
builder.Services.AddTypesenseClient(config =>
{
    config.ApiKey = typesenseApiKey;
    config.Nodes = new List<Node>
    {
        new(uri.Host, uri.Port.ToString(), uri.Scheme)
    };
});

builder.Services.AddOpenTelemetry().WithTracing(traceProvideBuilder =>
{
    traceProvideBuilder.SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService(builder.Environment.ApplicationName)).AddSource("Wolverine");
});

builder.Host.UseWolverine(options =>
{
    options.UseRabbitMqUsingNamedConnection("messaging").AutoProvision();
    options.ListenToRabbitQueue("questions.search", config => { config.BindExchange("questions"); });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();

app.MapGet("/search", async (string query, ITypesenseClient client) =>
{
    string? tag = null;
    var tagMatch = Regex.Match(query, @"\[(.*?)\]");
    if (tagMatch.Success)
    {
        tag = tagMatch.Groups[1].Value;
        query = query.Replace(tagMatch.Value, string.Empty).Trim();
    }

    var searchParams = new SearchParameters(query, "title,content");

    if (!string.IsNullOrEmpty(tag))
    {
        searchParams.FilterBy = $"tags:=[{tag}]";
    }

    try
    {
        var result = await client.Search<SearchQuestion>("questions", searchParams);
        return Results.Ok(result.Hits.Select(hit => hit.Document));
    }
    catch (Exception e)
    {
        return Results.Problem("Typesense search failed", e.Message);
    }
});

using var scope = app.Services.CreateScope();
var client = scope.ServiceProvider.GetRequiredService<ITypesenseClient>();
await SearchInitializer.EnsureIndexExistsAsync(client);

app.Run();