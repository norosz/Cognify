var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Cognify_Server>("api");

var web = builder.AddJavaScriptApp("web", "../cognify.client")
    .WithHttpEndpoint(port: 4200, env: "PORT")
    .WithReference(api);

builder.Build().Run();
