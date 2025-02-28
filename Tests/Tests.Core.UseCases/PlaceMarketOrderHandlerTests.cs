using BSDigital.MetaExchange.Core.UseCases;
using Moq;
using BSDigital.MetaExchange.Framework.UseCases;
using NUnit.Framework;

namespace BSDigital.MetaExchange.Tests.Core.UseCases;

internal class PlaceMarketOrderHandlerTests
{
    private Mock<IQueryHandler<GetBidsQuery, GetBidsResult>> _getBidsMock = null!;
    private Mock<IQueryHandler<GetAsksQuery, GetAsksResult>> _getAsksMock = null!;
    private Mock<IQueryHandler<GetExchangesQuery, GetExchangesResult>> _getExchangesMock = null!;
    private PlaceMarketOrderHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _getBidsMock = new Mock<IQueryHandler<GetBidsQuery, GetBidsResult>>();
        _getAsksMock = new Mock<IQueryHandler<GetAsksQuery, GetAsksResult>>();
        _getExchangesMock = new Mock<IQueryHandler<GetExchangesQuery, GetExchangesResult>>();
        _handler = new PlaceMarketOrderHandler(_getBidsMock.Object, _getAsksMock.Object, _getExchangesMock.Object);
    }

    private void SetupExchanges(params Exchange[] exchanges)
    {
        var exchangesDict = exchanges.ToDictionary(e => e.Id);
        _getExchangesMock.Setup(x => x.Handle(It.IsAny<GetExchangesQuery>()))
            .ReturnsAsync(new GetExchangesResult(exchangesDict));
    }

    private void SetupOrderBook(OrderType type, Dictionary<int, IEnumerable<BookOrder>> orders)
    {
        if (type == OrderType.Buy)
        {
            _getAsksMock.Setup(x => x.Handle(It.IsAny<GetAsksQuery>()))
                .ReturnsAsync(new GetAsksResult(orders));
        }
        else
        {
            _getBidsMock.Setup(x => x.Handle(It.IsAny<GetBidsQuery>()))
                .ReturnsAsync(new GetBidsResult(orders));
        }
    }

    private static Exchange CreateExchange(int id, decimal eurBalance, decimal btcBalance) =>
        new(id, eurBalance, btcBalance);

    private static BookOrder CreateOrder(decimal amount, decimal price, int exchangeId) =>
        new(amount, price, exchangeId);

    private static Dictionary<int, IEnumerable<BookOrder>> CreateOrderBook(
        params (int ExchangeId, BookOrder[] Orders)[] entries) =>
        entries.ToDictionary(e => e.ExchangeId, e => e.Orders.AsEnumerable());

    [Test]
    public async Task Handle_BasicBuyOrder_SingleExchange()
    {
        // Arrange
        SetupExchanges(CreateExchange(1, 0.00001m, 0m));
        SetupOrderBook(OrderType.Buy, CreateOrderBook(
            (1, [CreateOrder(0.00000001m, 1m, 1)])
        ));

        // Act
        var result = await _handler.Handle(new PlaceMarketOrderCommand(0.00000001m, OrderType.Buy));

        // Assert
        Assert.That(result.Orders.Count, Is.EqualTo(1));
        Assert.That(result.Orders[0].Amount, Is.EqualTo(0.00000001m));
        Assert.That(result.Orders[0].ExchangeId, Is.EqualTo(1));
    }

    [Test]
    public async Task Handle_BuyOrderSplitAcrossExchanges()
    {
        // Arrange
        SetupExchanges(
            CreateExchange(1, 0.00001m, 0m),
            CreateExchange(2, 0.00001m, 0m)
        );
        SetupOrderBook(OrderType.Buy, CreateOrderBook(
            (1, [CreateOrder(0.00000001m, 1m, 1)]),
            (2, [CreateOrder(0.00000001m, 1m, 2)])
        ));

        // Act
        var result = await _handler.Handle(new PlaceMarketOrderCommand(0.00000002m, OrderType.Buy));

        // Assert
        Assert.That(result.Orders.Count, Is.EqualTo(2));
        Assert.That(result.Orders.Sum(o => o.Amount), Is.EqualTo(0.00000002m));
        Assert.That(result.Orders[0].Amount, Is.EqualTo(0.00000001m));
        Assert.That(result.Orders[0].ExchangeId, Is.EqualTo(1));
        Assert.That(result.Orders[1].Amount, Is.EqualTo(0.00000001m));
        Assert.That(result.Orders[1].ExchangeId, Is.EqualTo(2));
    }

    [Test]
    public async Task Handle_InsufficientBalance_SkipsExchange()
    {
        // Arrange
        SetupExchanges(
            CreateExchange(1, 0m, 0m),
            CreateExchange(2, 0.00001m, 0m)
        );
        SetupOrderBook(OrderType.Buy, CreateOrderBook(
            (1, [CreateOrder(0.00000001m, 1m, 1)]),
            (2, [CreateOrder(0.00000001m, 1m, 2)])
        ));

        // Act
        var result = await _handler.Handle(new PlaceMarketOrderCommand(0.00000001m, OrderType.Buy));

        // Assert
        Assert.That(result.Orders.Count, Is.EqualTo(1));
        Assert.That(result.Orders[0].Amount, Is.EqualTo(0.00000001m));
        Assert.That(result.Orders[0].ExchangeId, Is.EqualTo(2));
    }

    [Test]
    public async Task Handle_EmptyOrderBooks_ReturnsEmptyPlan()
    {
        // Arrange
        SetupExchanges(CreateExchange(1, 0.00001m, 1m));
        SetupOrderBook(OrderType.Buy, new Dictionary<int, IEnumerable<BookOrder>>());

        // Act
        var result = await _handler.Handle(new PlaceMarketOrderCommand(0.00000001m, OrderType.Buy));

        // Assert
        Assert.That(result.Orders.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task Handle_PricePriority_ChoosesBestPrice()
    {
        // Arrange
        SetupExchanges(
            CreateExchange(1, 0.00001m, 0m),
            CreateExchange(2, 0.00001m, 0m)
        );
        SetupOrderBook(OrderType.Buy, CreateOrderBook(
            (1, [CreateOrder(0.00000001m, 1.1m, 1)]),
            (2, [CreateOrder(0.00000001m, 1m, 2)])
        ));

        // Act
        var result = await _handler.Handle(new PlaceMarketOrderCommand(0.00000001m, OrderType.Buy));

        // Assert
        Assert.That(result.Orders.Count, Is.EqualTo(1));
        Assert.That(result.Orders[0].Amount, Is.EqualTo(0.00000001m));
        Assert.That(result.Orders[0].ExchangeId, Is.EqualTo(2));
    }

    [Test]
    public async Task Handle_BasicSellOrder_SingleExchange()
    {
        // Arrange
        SetupExchanges(CreateExchange(1, 0.00001m, 0.00000001m));
        SetupOrderBook(OrderType.Sell, CreateOrderBook(
            (1, [CreateOrder(0.00000001m, 1m, 1)])
        ));

        // Act
        var result = await _handler.Handle(new PlaceMarketOrderCommand(0.00000001m, OrderType.Sell));

        // Assert
        Assert.That(result.Orders.Count, Is.EqualTo(1));
        Assert.That(result.Orders[0].Amount, Is.EqualTo(0.00000001m));
        Assert.That(result.Orders[0].ExchangeId, Is.EqualTo(1));
    }

    [Test]
    public async Task Handle_PartialFill_ContinuesWithNextOrder()
    {
        // Arrange
        SetupExchanges(CreateExchange(1, 0.00001m, 0m));
        SetupOrderBook(OrderType.Buy, CreateOrderBook(
            (1, [
                CreateOrder(0.00000003m, 1m, 1),
                CreateOrder(0.00000007m, 1.1m, 1)
            ])
        ));

        // Act
        var result = await _handler.Handle(new PlaceMarketOrderCommand(0.00000005m, OrderType.Buy));

        // Assert
        Assert.That(result.Orders.Count, Is.EqualTo(1));
        Assert.That(result.Orders[0].Amount, Is.EqualTo(0.00000005m));
        Assert.That(result.Orders[0].ExchangeId, Is.EqualTo(1));
    }

    [Test]
    public async Task Handle_ConsolidatesOrdersFromSameExchange()
    {
        // Arrange
        SetupExchanges(CreateExchange(1, 0.00001m, 0m));
        SetupOrderBook(OrderType.Buy, CreateOrderBook(
            (1, [
                CreateOrder(0.00000003m, 1m, 1),
                CreateOrder(0.00000003m, 1m, 1),
                CreateOrder(0.00000004m, 1m, 1)
            ])
        ));

        // Act
        var result = await _handler.Handle(new PlaceMarketOrderCommand(0.00000010m, OrderType.Buy));

        // Assert
        Assert.That(result.Orders.Count, Is.EqualTo(1));
        Assert.That(result.Orders[0].Amount, Is.EqualTo(0.00000010m));
        Assert.That(result.Orders[0].ExchangeId, Is.EqualTo(1));
    }

    [Test]
    public async Task Handle_InsufficientCryptoBalance_SkipsExchange()
    {
        // Arrange
        SetupExchanges(
            CreateExchange(1, 0.00001m, 0m),
            CreateExchange(2, 0.00001m, 0.00000001m)
        );
        SetupOrderBook(OrderType.Sell, CreateOrderBook(
            (1, [CreateOrder(0.00000001m, 1m, 1)]),
            (2, [CreateOrder(0.00000001m, 1m, 2)])
        ));

        // Act
        var result = await _handler.Handle(new PlaceMarketOrderCommand(0.00000001m, OrderType.Sell));

        // Assert
        Assert.That(result.Orders.Count, Is.EqualTo(1));
        Assert.That(result.Orders[0].Amount, Is.EqualTo(0.00000001m));
        Assert.That(result.Orders[0].ExchangeId, Is.EqualTo(2));
    }

    [Test]
    public async Task Handle_ZeroAmount_ReturnsEmptyPlan()
    {
        // Arrange
        SetupExchanges(CreateExchange(1, 0.00001m, 0m));
        SetupOrderBook(OrderType.Buy, CreateOrderBook(
            (1, [CreateOrder(0.00000001m, 1m, 1)])
        ));

        // Act
        var result = await _handler.Handle(new PlaceMarketOrderCommand(0m, OrderType.Buy));

        // Assert
        Assert.That(result.Orders.Count, Is.EqualTo(0));
    }

    [Test]
    [Ignore("Not implemented yet")]
    public async Task Handle_SamePriceMultipleExchanges_PreferLowerBalance()
    {
        // Arrange
        SetupExchanges(
            CreateExchange(1, 0.00001m, 0m),
            CreateExchange(2, 0.000005m, 0m)
        );
        SetupOrderBook(OrderType.Buy, CreateOrderBook(
            (1, [CreateOrder(0.00000001m, 1m, 1)]),
            (2, [CreateOrder(0.00000001m, 1m, 2)])
        ));

        // Act
        var result = await _handler.Handle(new PlaceMarketOrderCommand(0.00000001m, OrderType.Buy));

        // Assert
        Assert.That(result.Orders.Count, Is.EqualTo(1));
        Assert.That(result.Orders[0].Amount, Is.EqualTo(0.00000001m));
        Assert.That(result.Orders[0].ExchangeId, Is.EqualTo(2));
    }

    [Test]
    public async Task Handle_InsufficientBalanceForPrice_SkipsOrder()
    {
        // Arrange
        SetupExchanges(CreateExchange(1, 0.000000001m, 0m));
        SetupOrderBook(OrderType.Buy, CreateOrderBook(
            (1, [
                CreateOrder(0.00000001m, 1m, 1),
                CreateOrder(0.00000001m, 1000m, 1)
            ])
        ));

        // Act
        var result = await _handler.Handle(new PlaceMarketOrderCommand(0.00000001m, OrderType.Buy));

        // Assert
        Assert.That(result.Orders.Count, Is.EqualTo(1));
        Assert.That(result.Orders[0].Amount, Is.EqualTo(0.000000001m));
        Assert.That(result.Orders[0].ExchangeId, Is.EqualTo(1));
    }

    [Test]
    public async Task Handle_MixedPriceLevels_TakesLowestFirst()
    {
        // Arrange
        SetupExchanges(
            CreateExchange(1, 0.00001m, 0m),
            CreateExchange(2, 0.00001m, 0m)
        );
        SetupOrderBook(OrderType.Buy, CreateOrderBook(
            (1, [
                CreateOrder(0.00000001m, 1.0m, 1),
                CreateOrder(0.00000001m, 1.2m, 1),
            ]),
            (2, [
                CreateOrder(0.00000001m, 1.1m, 2)
            ])
        ));

        // Act
        var result = await _handler.Handle(new PlaceMarketOrderCommand(0.00000002m, OrderType.Buy));

        // Assert
        Assert.That(result.Orders.Count, Is.EqualTo(2));
        Assert.That(result.Orders[0].Amount, Is.EqualTo(0.00000001m));
        Assert.That(result.Orders[0].ExchangeId, Is.EqualTo(1));
        Assert.That(result.Orders[1].Amount, Is.EqualTo(0.00000001m));
        Assert.That(result.Orders[1].ExchangeId, Is.EqualTo(2));
    }

    [Test]
    public async Task Handle_MaximumDecimalPrecision_HandlesCorrectly()
    {
        // Arrange
        const decimal smallestAmount = 0.0000000000000000000000000001m;
        SetupExchanges(CreateExchange(1, 0.00001m, 0m));
        SetupOrderBook(OrderType.Buy, CreateOrderBook(
            (1, [CreateOrder(smallestAmount, 1m, 1)])
        ));

        // Act
        var result = await _handler.Handle(new PlaceMarketOrderCommand(smallestAmount, OrderType.Buy));

        // Assert
        Assert.That(result.Orders.Count, Is.EqualTo(1));
        Assert.That(result.Orders[0].Amount, Is.EqualTo(smallestAmount));
        Assert.That(result.Orders[0].ExchangeId, Is.EqualTo(1));
    }

    [Test]
    public async Task Handle_SellOrderSplitAcrossExchanges()
    {
        // Arrange
        SetupExchanges(
            CreateExchange(1, 0.00001m, 0.00000001m),
            CreateExchange(2, 0.00001m, 0.00000001m)
        );
        SetupOrderBook(OrderType.Sell, CreateOrderBook(
            (1, [CreateOrder(0.00000001m, 1m, 1)]),
            (2, [CreateOrder(0.00000001m, 1m, 2)])
        ));

        // Act
        var result = await _handler.Handle(new PlaceMarketOrderCommand(0.00000002m, OrderType.Sell));

        // Assert
        Assert.That(result.Orders.Count, Is.EqualTo(2));
        Assert.That(result.Orders.Sum(o => o.Amount), Is.EqualTo(0.00000002m));
        Assert.That(result.Orders[0].Amount, Is.EqualTo(0.00000001m));
        Assert.That(result.Orders[1].Amount, Is.EqualTo(0.00000001m));
    }

    [Test]
    public async Task Handle_SellOrder_TakesHighestBidFirst()
    {
        // Arrange
        SetupExchanges(
            CreateExchange(1, 0.00001m, 0.00000002m),
            CreateExchange(2, 0.00001m, 0.00000002m)
        );
        SetupOrderBook(OrderType.Sell, CreateOrderBook(
            (1, [CreateOrder(0.00000001m, 1.0m, 1)]),
            (2, [CreateOrder(0.00000001m, 1.1m, 2)])
        ));

        // Act
        var result = await _handler.Handle(new PlaceMarketOrderCommand(0.00000001m, OrderType.Sell));

        // Assert
        Assert.That(result.Orders.Count, Is.EqualTo(1));
        Assert.That(result.Orders[0].Amount, Is.EqualTo(0.00000001m));
        Assert.That(result.Orders[0].ExchangeId, Is.EqualTo(2));
    }

    [Test]
    public async Task Handle_SellOrder_PartialFill()
    {
        // Arrange
        SetupExchanges(CreateExchange(1, 0.00001m, 0.00000005m));
        SetupOrderBook(OrderType.Sell, CreateOrderBook(
            (1, [
                CreateOrder(0.00000003m, 1.1m, 1),
                CreateOrder(0.00000002m, 1.0m, 1)
            ])
        ));

        // Act
        var result = await _handler.Handle(new PlaceMarketOrderCommand(0.00000004m, OrderType.Sell));

        // Assert
        Assert.That(result.Orders.Count, Is.EqualTo(1));
        Assert.That(result.Orders[0].Amount, Is.EqualTo(0.00000004m));
        Assert.That(result.Orders[0].ExchangeId, Is.EqualTo(1));
    }

    [Test]
    public async Task Handle_SellOrder_MixedPriceLevels()
    {
        // Arrange
        SetupExchanges(
            CreateExchange(1, 0.00001m, 0.00000003m),
            CreateExchange(2, 0.00001m, 0.00000003m)
        );
        SetupOrderBook(OrderType.Sell, CreateOrderBook(
            (1, [
                CreateOrder(0.00000001m, 1.2m, 1),
                CreateOrder(0.00000001m, 1.0m, 1),
            ]),
            (2, [CreateOrder(0.00000001m, 1.1m, 2)])
        ));

        // Act
        var result = await _handler.Handle(new PlaceMarketOrderCommand(0.00000002m, OrderType.Sell));

        // Assert
        Assert.That(result.Orders.Count, Is.EqualTo(2));
        Assert.That(result.Orders[0].Amount, Is.EqualTo(0.00000001m));
        Assert.That(result.Orders[0].ExchangeId, Is.EqualTo(1)); // Takes from exchange with highest price first
        Assert.That(result.Orders[1].Amount, Is.EqualTo(0.00000001m));
        Assert.That(result.Orders[1].ExchangeId, Is.EqualTo(2));
    }
}