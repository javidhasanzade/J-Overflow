using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using QuestionService.Data;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.AddServiceDefaults();
builder.Services.AddAuthentication()
    .AddKeycloakJwtBearer(serviceName: "keycloack", realm: "joverflow", options =>
    {
        options.RequireHttpsMetadata = false;
        options.Audience = "joverflow";
    });
builder.AddNpgsqlDbContext<QuestionDbContext>("questionDb");

builder.Services.AddOpenTelemetry().WithTracing(traceProvideBuilder =>
{
    traceProvideBuilder.SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService(builder.Environment.ApplicationName)).AddSource("Wolverine");
});

builder.Host.UseWolverine(options =>
{
    options.UseRabbitMqUsingNamedConnection("messaging").AutoProvision();
    options.PublishAllMessages().ToRabbitExchange("questions");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapDefaultEndpoints();

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
try
{
    var context = services.GetRequiredService<QuestionDbContext>();
    await context.Database.MigrateAsync();
}
catch (Exception e)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(e, "An error occurred while migrating or seeding the DB.");
}

app.Run();