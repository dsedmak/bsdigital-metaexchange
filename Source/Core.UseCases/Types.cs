using BSDigital.MetaExchange.Framework.UseCases;


namespace BSDigital.MetaExchange.Core.UseCases;

public record GetBidsQuery() : IQuery<GetBidsResult>;

public record GetBidsResult(Dictionary<int, IEnumerable<BookOrder>> OrderBooksByExchangeId);

public record GetAsksQuery() : IQuery<GetAsksResult>;

public record GetAsksResult(Dictionary<int, IEnumerable<BookOrder>> OrderBooksByExchangeId);

public record GetExchangesQuery : IQuery<GetExchangesResult>;

public record GetExchangesResult(Dictionary<int, Exchange> ExchangesById);

public record BookOrder(decimal Amount, decimal Price, int ExchangeId);

public record Order(decimal Amount, int ExchangeId, OrderType Type);

public enum OrderType
{
    Buy,
    Sell
}

public record Exchange(int Id, decimal EurBalance, decimal BtcBalance);

public record PlaceMarketOrderCommand(decimal Amount, OrderType OrderType)
    : ICommand<PlaceMarketOrderResult>;

public record PlaceMarketOrderResult(List<Order> Orders);