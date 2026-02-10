using Typesense.Setup;

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();

app.Run();