var builder = DistributedApplication.CreateBuilder(args);

var password = builder.AddParameter("sql-password");

var sqldb = builder.AddSqlServer("sql", password, 14333)
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("sqldata");

var api = builder.AddProject<Projects.Cognify_Server>("api")
    .WithReference(sqldb);

var web = builder.AddJavaScriptApp("web", "../cognify.client")
    .WithHttpEndpoint(port: 4200, env: "PORT")
    .WithReference(api);

builder.Build().Run();
