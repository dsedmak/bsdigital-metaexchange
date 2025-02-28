using BSDigital.MetaExchange.Core.Db.EfCore.SQLite;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSqLiteDbMigrationsServices("Data Source=Hosts.WebHost.db");
var host = builder.Build();
host.Run();