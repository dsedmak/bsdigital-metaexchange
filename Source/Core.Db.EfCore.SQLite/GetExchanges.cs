using BSDigital.Injectable;
using BSDigital.MetaExchange.Core.UseCases;
using BSDigital.MetaExchange.Framework.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace BSDigital.MetaExchange.Core.Db.EfCore.SQLite;

[Singleton<GetExchanges, IQueryHandler<GetExchangesQuery, GetExchangesResult>>]
internal class GetExchanges : IQueryHandler<GetExchangesQuery, GetExchangesResult>
{
    private readonly IDbContextFactory<MetaExchangeDbContext> _dbContextFactory;

    public GetExchanges(IDbContextFactory<MetaExchangeDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async ValueTask<GetExchangesResult> Handle(GetExchangesQuery query)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var dict = await dbContext.Exchanges.AsNoTracking().ToDictionaryAsync(exchange => exchange.Id,
            exchange => new UseCases.Exchange(exchange.Id, exchange.EurBalance, exchange.BtcBalance));
        return new GetExchangesResult(dict);
    }
}