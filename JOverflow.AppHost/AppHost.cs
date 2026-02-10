var builder = DistributedApplication.CreateBuilder(args);

#pragma warning disable ASPIRECERTIFICATES001
var keycloak = builder.AddKeycloak("keycloack", 6001)
    .WithoutHttpsCertificate()
    .WithDataVolume("keycloack-data");
#pragma warning restore ASPIRECERTIFICATES001

var postgres = builder.AddPostgres("postgres", port: 5433)
    .WithDataVolume("postgres-data")
    .WithPgAdmin();

var typesenseApiKey = builder.AddParameter("typesense-api-key", secret: true);

var typesense = builder.AddContainer("typesense", "typesense/typesense", "29.0")
    .WithArgs("--data-dir", "/data", "--api-key", typesenseApiKey, "--enable-cors")
    .WithVolume("typesense-data", "/data")
    .WithHttpEndpoint(8108, 8108, name: "typesense");

var typesenseContainer = typesense.GetEndpoint("typesense");

var questionDb = postgres.AddDatabase("questionDb");

var questionService = builder.AddProject<Projects.QuestionService>("question-service")
    .WithReference(keycloak)
    .WithReference(questionDb)
    .WaitFor(keycloak)
    .WaitFor(questionDb);

var searchService = builder.AddProject<Projects.SearchService>("search-service")
    .WithEnvironment("typesense-api-key", typesenseApiKey)
    .WithReference(typesenseContainer)
    .WaitFor(typesense);

builder.Build().Run();