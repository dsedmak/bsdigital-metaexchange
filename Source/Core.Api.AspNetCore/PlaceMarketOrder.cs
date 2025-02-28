using BSDigital.MetaExchange.Core.UseCases;
using BSDigital.MetaExchange.Framework.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BSDigital.MetaExchange.Core.Api.AspNetCore;

public record PlaceMarketOrderRequest(decimal Amount, OrderType OrderType);

public record PlaceMarketOrderResponse(List<Order> Orders);

[ApiController]
[Route("execute-order")]
[AllowAnonymous]
[EnableRateLimiting("onatatime")]
public class PlaceMarketOrderController : ControllerBase
{
    private readonly ICommandHandler<PlaceMarketOrderCommand, PlaceMarketOrderResult> _commandHandler;

    public PlaceMarketOrderController(ICommandHandler<PlaceMarketOrderCommand, PlaceMarketOrderResult> commandHandler)
    {
        _commandHandler = commandHandler;
    }

    [HttpPost]
    public async ValueTask<PlaceMarketOrderResponse> ExecuteAsync(PlaceMarketOrderRequest request)
    {
        // TODO: better validation
        var command = new PlaceMarketOrderCommand(request.Amount, request.OrderType);
        var result = await _commandHandler.Handle(command);
        return new PlaceMarketOrderResponse(result.Orders);
    }
}