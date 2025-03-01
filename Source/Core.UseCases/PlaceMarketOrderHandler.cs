using BSDigital.Injectable;
using BSDigital.MetaExchange.Framework.UseCases;

namespace BSDigital.MetaExchange.Core.UseCases;

[Singleton<PlaceMarketOrderHandler, ICommandHandler<PlaceMarketOrderCommand, PlaceMarketOrderResult>>]
internal class PlaceMarketOrderHandler : ICommandHandler<PlaceMarketOrderCommand, PlaceMarketOrderResult>
{
    private readonly IQueryHandler<GetBidsQuery, GetBidsResult> _getBids;
    private readonly IQueryHandler<GetAsksQuery, GetAsksResult> _getAsks;
    private readonly IQueryHandler<GetExchangesQuery, GetExchangesResult> _getExchanges;

    public PlaceMarketOrderHandler(IQueryHandler<GetBidsQuery, GetBidsResult> getBids,
        IQueryHandler<GetAsksQuery, GetAsksResult> getAsks,
        IQueryHandler<GetExchangesQuery, GetExchangesResult> getExchanges)
    {
        _getBids = getBids;
        _getAsks = getAsks;
        _getExchanges = getExchanges;
    }

    public async ValueTask<PlaceMarketOrderResult> Handle(PlaceMarketOrderCommand command)
    {
        var (exchanges, orderBooksByExchangeId) = await GetData(command);

        var orderQueue = BuildPriorityQueue(command, orderBooksByExchangeId, exchanges);

        var executionPlan = ResolveExecutionPlan(command, orderQueue, exchanges, orderBooksByExchangeId);

        // TODO: save the changed state of exchanges

        return new PlaceMarketOrderResult(executionPlan);
    }

    private async Task<(
        Dictionary<int, Exchange> exchanges,
        Dictionary<int, IEnumerator<BookOrder>> orderBooksByExchangeId
        )> GetData(PlaceMarketOrderCommand command)
    {
        var exchanges = (await _getExchanges.Handle(new GetExchangesQuery())).ExchangesById;

        var orderBooksByExchangeId = (command.OrderType switch
        {
            OrderType.Sell => (await _getBids.Handle(new GetBidsQuery())).OrderBooksByExchangeId,
            OrderType.Buy => (await _getAsks.Handle(new GetAsksQuery())).OrderBooksByExchangeId,
        }).ToDictionary(kv => kv.Key, kv => kv.Value.GetEnumerator());

        return (exchanges, orderBooksByExchangeId);
    }

    private record struct Priority(decimal Price, decimal Balance);

    private static PriorityQueue<BookOrder, Priority> BuildPriorityQueue(
        PlaceMarketOrderCommand command,
        Dictionary<int, IEnumerator<BookOrder>> orderBooksByExchangeId,
        Dictionary<int, Exchange> exchanges)
    {
        // tie breaking with balance allows us to equalize balances across exchanges over time
        var orderQueue = new PriorityQueue<BookOrder, Priority>(command.OrderType switch
        {
            OrderType.Sell => Comparer<Priority>.Create((a, b) =>
                b.Price.CompareTo(a.Price) != 0
                    ? b.Price.CompareTo(a.Price)
                    : b.Balance.CompareTo(a.Balance)), // highest price first, then highest balance
            OrderType.Buy => Comparer<Priority>.Create((a, b) =>
                a.Price.CompareTo(b.Price) != 0
                    ? a.Price.CompareTo(b.Price)
                    : b.Balance.CompareTo(a.Balance)), // lowest price first, then highest balance
        });

        // enqueue only the best offer for now
        // here we assume the order books are sorted which seems likely
        foreach (var (_, enumerator) in orderBooksByExchangeId)
        {
            if (!enumerator.MoveNext()) continue;
            var exchange = exchanges[enumerator.Current.ExchangeId];
            var priority = ResolvePriority(enumerator.Current, command.OrderType, exchange);
            orderQueue.Enqueue(enumerator.Current, priority);
        }

        return orderQueue;
    }

    private static Priority ResolvePriority(BookOrder order, OrderType orderType, Exchange exchange)
    {
        return new Priority(order.Price, orderType switch
        {
            OrderType.Buy => exchange.EurBalance,
            OrderType.Sell => exchange.BtcBalance
        });
    }

    private static List<Order> ResolveExecutionPlan(
        PlaceMarketOrderCommand command,
        PriorityQueue<BookOrder, Priority> orderQueue,
        Dictionary<int, Exchange> exchanges,
        Dictionary<int, IEnumerator<BookOrder>> orderBooksByExchangeId)
    {
        var executionPlan = new List<Order>();
        var remainingAmount = command.Amount;

        while (remainingAmount > 0 && orderQueue.TryDequeue(out var order, out _))
        {
            var exchange = exchanges[order.ExchangeId];
            var fillAmount = Math.Min(remainingAmount, Math.Min(order.Amount, command.OrderType switch
            {
                OrderType.Buy => exchange.EurBalance / order.Price,
                OrderType.Sell => exchange.BtcBalance,
            }));

            if (fillAmount <= 0) continue; // could happen due to no balance
            exchanges[order.ExchangeId] = ResolveNewExchangeBalance(command.OrderType, exchange, fillAmount, order);
            executionPlan.Add(new Order(fillAmount, order.ExchangeId, command.OrderType));
            remainingAmount -= fillAmount;

            ReplaceDequeuedOrder(fillAmount, order, exchange);
        }

        // we can combine multiple market orders into one to reduce fees
        return executionPlan
            .GroupBy(o => o.ExchangeId)
            .Select(g => new Order(g.Sum(o => o.Amount), g.Key, command.OrderType))
            .ToList();

        void ReplaceDequeuedOrder(decimal fillAmount, BookOrder order, Exchange exchange)
        {
            if (fillAmount < order.Amount) // if we haven't fully consumed a book order put the remainder back
            {
                var remainingOrder = order with { Amount = order.Amount - fillAmount };
                var priority = ResolvePriority(remainingOrder, command.OrderType, exchange);
                orderQueue.Enqueue(remainingOrder, priority);
            }
            else // if we have enqueue the next one from the same exchange so it's available
            {
                var enumerator = orderBooksByExchangeId[order.ExchangeId];
                if (!enumerator.MoveNext()) return;
                var priority = ResolvePriority(enumerator.Current, command.OrderType, exchanges[order.ExchangeId]);
                orderQueue.Enqueue(enumerator.Current, priority);
            }
        }

        static Exchange ResolveNewExchangeBalance(OrderType orderType, Exchange exchange, decimal fillAmount, BookOrder order)
        {
            return orderType switch
            {
                OrderType.Buy => exchange with
                {
                    BtcBalance = exchange.BtcBalance + fillAmount,
                    EurBalance = exchange.EurBalance - fillAmount * order.Price,
                },
                OrderType.Sell => exchange with
                {
                    BtcBalance = exchange.BtcBalance - fillAmount,
                    EurBalance = exchange.EurBalance + fillAmount * order.Price,
                },
            };
        }
    }
}