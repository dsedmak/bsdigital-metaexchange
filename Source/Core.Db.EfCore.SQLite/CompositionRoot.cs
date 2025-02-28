using BSDigital.Injectable;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BSDigital.MetaExchange.Core.Db.EfCore.SQLite;

public static class CompositionRoot
{
    public static void AddSqLiteDbMigrationsServices(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<MetaExchangeDbContext>(options => options.UseSqlite(connectionString));
    }

    public static void AddSqLiteDb(this IServiceCollection services, Action<SqLiteDbOptions> options)
    {
        var dbOptions = new SqLiteDbOptions();
        options(dbOptions);
        services.Configure(options);
        services.AddDbContextFactory<MetaExchangeDbContext>(o => o.UseSqlite(dbOptions.ConnectionString));
        services.RegisterInjectableTypes(typeof(CompositionRoot).Assembly);
    }

    public static void EnsureDatabaseCreated(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MetaExchangeDbContext>();
        context.Database.Migrate(); // dotnet-ef cannot handle async
    }
}
    
public class SqLiteDbOptions
{
    public string? ConnectionString { get; set; }
    public string? OrderBooksPath { get; set; }
}