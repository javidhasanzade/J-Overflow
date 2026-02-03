var builder = DistributedApplication.CreateBuilder(args);

var keycloack = builder.AddKeycloak("keycloack", 6001).WithDataVolume("keycloack-data");

builder.Build().Run();