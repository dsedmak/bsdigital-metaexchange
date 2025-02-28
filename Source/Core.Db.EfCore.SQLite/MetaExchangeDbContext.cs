using Microsoft.EntityFrameworkCore;

namespace BSDigital.MetaExchange.Core.Db.EfCore.SQLite;

internal class MetaExchangeDbContext : DbContext
{
    public MetaExchangeDbContext(DbContextOptions<MetaExchangeDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSeeding((context, _) =>
        {
            if (context.Set<Exchange>().Any()) return;
            context.Set<Exchange>().AddRange(Enumerable.Range(1, 31).Select(i => new Exchange
            {
                Id = i,
                BtcBalance = 10m,
                EurBalance = 10000m
            }));
            context.SaveChanges();
        });
    }

    public DbSet<Exchange> Exchanges { get; init; } = null!;
}

internal class Exchange
{
    public required int Id { get; init; }
    public required decimal EurBalance { get; init; }
    public required decimal BtcBalance { get; init; }
}