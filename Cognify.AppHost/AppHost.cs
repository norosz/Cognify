var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
                 .WithDataVolume();
var sqldb = sql.AddDatabase("sqldb");

var api = builder.AddProject<Projects.Cognify_Server>("api")
    .WithReference(sqldb);

var web = builder.AddJavaScriptApp("web", "../cognify.client")
    .WithHttpEndpoint(port: 4200, env: "PORT")
    .WithReference(api);

builder.Build().Run();
