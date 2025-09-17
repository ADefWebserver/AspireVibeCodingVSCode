var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.BlazorWebApp>("BlazorWebApp");

builder.Build().Run();