var builder = DistributedApplication.CreateBuilder(args);

var password = builder.AddParameter("sql-password", "Password123!");

var sqldb = builder.AddSqlServer("sql", password, 14333)
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("sqldata");

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(r => r.WithDataVolume("azurite-data"));

var blobs = storage.AddBlobs("blobs");

var api = builder.AddProject<Projects.Cognify_Server>("api")
    .WithReference(sqldb)
    .WithReference(blobs);

var web = builder.AddJavaScriptApp("web", "../cognify.client")
    .WithHttpEndpoint(port: 4200, env: "PORT")
    .WithReference(api);

builder.Build().Run();
