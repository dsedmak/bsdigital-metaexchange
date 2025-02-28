using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using BSDigital.Injectable;
using BSDigital.MetaExchange.Core.UseCases;
using BSDigital.MetaExchange.Framework.UseCases;
using Microsoft.Extensions.Options;


namespace BSDigital.MetaExchange.Core.Db.EfCore.SQLite;

[Singleton<FileLoader, IQueryHandler<GetBidsQuery, GetBidsResult>>]
[Singleton<FileLoader, IQueryHandler<GetAsksQuery, GetAsksResult>>]
internal partial class FileLoader : IQueryHandler<GetAsksQuery, GetAsksResult>,
    IQueryHandler<GetBidsQuery, GetBidsResult>
{
    private readonly IOptions<SqLiteDbOptions> _options;

    public FileLoader(IOptions<SqLiteDbOptions> options)
    {
        _options = options;
    }
    
    [GeneratedRegex(@"\d+\.\d+\t")]
    private static partial Regex UnixTimeTabRegex();

    public async ValueTask<GetAsksResult> Handle(GetAsksQuery query)
    {
        var result = await LoadData((ob, exchangeId) =>
            ob.Asks.Select(entry => new BookOrder(entry.Order.Amount, entry.Order.Price, exchangeId)));
        return new GetAsksResult(result);
    }

    public async ValueTask<GetBidsResult> Handle(GetBidsQuery query)
    {
        var result = await LoadData((ob, exchangeId) =>
            ob.Bids.Select(entry => new BookOrder(entry.Order.Amount, entry.Order.Price, exchangeId)));
        return new GetBidsResult(result);
    }

    private async ValueTask<Dictionary<int, IEnumerable<BookOrder>>> LoadData(
        Func<OrderBook, int, IEnumerable<BookOrder>> selector)
    {
        var stripUnixTime = UnixTimeTabRegex();
        var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } };

        var stepCounter = 0;
        var lineCounter = 0;

        var path = _options.Value.OrderBooksPath;

        var result = new Dictionary<int, IEnumerable<BookOrder>>();
        await foreach (var line in File.ReadLinesAsync(path ?? throw new ArgumentNullException("OrderBooksPath was not provided")))
        {
            if (lineCounter % 100 == 0)
            {
                stepCounter++;
                var orderBook = JsonSerializer.Deserialize<OrderBook>(stripUnixTime.Replace(line, ""), options);
                result.Add(stepCounter, selector(orderBook!, stepCounter));
            }

            lineCounter++;
        }

        return result;
    }

    public record Ask(Order Order);

    public record Bid(Order Order);

    public record Order(object? Id, DateTime Time, OrderType Type, OrderKind Kind, decimal Amount, decimal Price);

    public record OrderBook(DateTime AcqTime, List<Bid> Bids, List<Ask> Asks);

    public enum OrderType
    {
        Buy,
        Sell
    }

    public enum OrderKind
    {
        Market,
        Limit
    }
}