var builder = DistributedApplication.CreateBuilder(args);

var keycloack = builder.AddKeycloak("keycloack", 6001).WithDataVolume("keycloack-data");

var questionService = builder.AddProject<Projects.QuestionService>("question-service")
    .WithReference(keycloack)
    .WaitFor(keycloack);

builder.Build().Run();