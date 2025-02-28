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
        var exchanges = (await _getExchanges.Handle(new GetExchangesQuery())).ExchangesById;
        var orderBooksByExchangeId = (command.OrderType switch
        {
            OrderType.Sell => (await _getBids.Handle(new GetBidsQuery())).OrderBooksByExchangeId,
            OrderType.Buy => (await _getAsks.Handle(new GetAsksQuery())).OrderBooksByExchangeId,
        }).ToDictionary(kv => kv.Key, kv => kv.Value.GetEnumerator());

        var orderQueue = new PriorityQueue<BookOrder, decimal>(command.OrderType switch
        {
            OrderType.Sell => Comparer<decimal>.Create((a, b) => b.CompareTo(a)), // we want highest bid first
            OrderType.Buy => Comparer<decimal>.Create((a, b) => a.CompareTo(b)), // we want lowest ask first
        });

        foreach (var (_, enumerator) in orderBooksByExchangeId)
        {
            EnqueueFromEnumerator(enumerator, orderQueue);
        }

        var executionPlan = ResolveExecutionPlan(command, orderQueue, exchanges, orderBooksByExchangeId);

        // TODO: save the changed state of exchanges

        return new PlaceMarketOrderResult(executionPlan);
    }

    private static List<Order> ResolveExecutionPlan(
        PlaceMarketOrderCommand command,
        PriorityQueue<BookOrder, decimal> orderQueue,
        Dictionary<int, Exchange> exchanges,
        Dictionary<int, IEnumerator<BookOrder>> orderBooksByExchangeId)
    {
        var executionPlan = new List<Order>();
        var remainingAmount = command.Amount;
        var currentExchangeId = -1;
        var currentExchangeAmount = 0m;

        while (remainingAmount > 0 && orderQueue.Count > 0)
        {
            var order = orderQueue.Dequeue();
            var exchange = exchanges[order.ExchangeId];
            var fillAmount = Math.Min(remainingAmount, Math.Min(order.Amount, command.OrderType switch
            {
                OrderType.Buy => exchange.EurBalance / order.Price,
                OrderType.Sell => exchange.BtcBalance,
            }));

            if (fillAmount <= 0) // could happen due to no balance
            {
                continue;
            }

            exchanges[order.ExchangeId] = UpdateExchangeBalance(command.OrderType, exchange, fillAmount, order);

            if (currentExchangeId != -1 && currentExchangeId != order.ExchangeId)
            {
                executionPlan.Add(new Order(currentExchangeAmount, currentExchangeId, command.OrderType));
                currentExchangeAmount = 0;
            }

            currentExchangeId = order.ExchangeId;
            remainingAmount -= fillAmount;
            currentExchangeAmount += fillAmount;

            if (fillAmount < order.Amount) // if we haven't fully consumed a book order put the remainder back
            {
                var remainingOrder = order with { Amount = order.Amount - fillAmount };
                orderQueue.Enqueue(remainingOrder, remainingOrder.Price);
            }
            else // if we have enqueue the next one from the same exchange so it's available
            {
                var enumerator = orderBooksByExchangeId[order.ExchangeId];
                EnqueueFromEnumerator(enumerator, orderQueue);
            }
        }

        if (currentExchangeAmount > 0)
        {
            executionPlan.Add(new Order(currentExchangeAmount, currentExchangeId, command.OrderType));
        }

        // we can combine multiple market orders into one to reduce fees
        executionPlan = executionPlan.GroupBy(o => o.ExchangeId)
            .Select(g => new Order(g.Sum(o => o.Amount), g.Key, command.OrderType)).ToList();
        return executionPlan;

        static Exchange UpdateExchangeBalance(OrderType orderType, Exchange exchange, decimal fillAmount, BookOrder order)
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
                    BtcBalance = exchange.BtcBalance + fillAmount,
                    EurBalance = exchange.EurBalance - fillAmount * order.Price,
                },
            };
        }
    }

    /*
     * TODO: we can enqueue all book orders with the same price then give priority to exchanges with lower balances
     * This would in effect continuously equalize balances between exchanges
     */
    private static void EnqueueFromEnumerator(IEnumerator<BookOrder> enumerator,
        PriorityQueue<BookOrder, decimal> priorityQueue)
    {
        if (enumerator.MoveNext())
        {
            priorityQueue.Enqueue(enumerator.Current, enumerator.Current.Price);
        }
    }
}