using BSDigital.MetaExchange.Core.Db.EfCore.SQLite;
using BSDigital.MetaExchange.Core.UseCases;
using BSDigital.MetaExchange.Framework.UseCases;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

if (args.Length != 3)
{
    Console.WriteLine("Usage: ConsoleHost <buy|sell> <amount> <order-books-file-path>");
    return 1;
}

if (!Enum.TryParse<OrderType>(args[0], true, out var orderType))
{
    Console.WriteLine("First argument must be either 'buy' or 'sell'");
    return 1;
}

if (!decimal.TryParse(args[1], out var amount) || amount <= 0)
{
    Console.WriteLine("Second argument must be a positive number");
    return 1;
}

var orderBooksPath = args[2];
if (!File.Exists(orderBooksPath))
{
    Console.WriteLine("Third argument must be a valid file path");
    return 1;
}

var services = new ServiceCollection();
services.AddSqLiteDb(options =>
{
    options.ConnectionString = "Data Source=Hosts.ConsoleHost.db";
    options.OrderBooksPath = orderBooksPath;
});
services.AddUseCases();

var provider = services.BuildServiceProvider();
provider.EnsureDatabaseCreated();

var handler = provider.GetRequiredService<ICommandHandler<PlaceMarketOrderCommand, PlaceMarketOrderResult>>();
var result = await handler.Handle(new PlaceMarketOrderCommand(amount, orderType));

Console.WriteLine($"Execution plan for {orderType.ToString().ToLower()}ing {amount} BTC:");
foreach (var order in result.Orders)
{
    Console.WriteLine($"- Exchange {order.ExchangeId}: {order.Amount:F8} BTC");
}

return 0;