var builder = DistributedApplication.CreateBuilder(args);

var password = builder.AddParameter("sql-password", "Password123!");

var sqldb = builder.AddSqlServer("sql", password, 14333)
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("sqldata");

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite =>
    {
        azurite.WithDataVolume()
            .WithLifetime(ContainerLifetime.Persistent);
    });

var blobs = storage.AddBlobs("blobs");

var api = builder.AddProject<Projects.Cognify_Server>("api")
    .WithReference(sqldb)
    .WaitFor(sqldb)
    .WithReference(blobs)
    .WaitFor(storage)
    .WithEnvironment("OpenAI:ApiKey", builder.AddParameter("openai-key", secret: true));

var web = builder.AddJavaScriptApp("web", "../cognify.client")
    .WithHttpEndpoint(port: 4200, env: "PORT")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
