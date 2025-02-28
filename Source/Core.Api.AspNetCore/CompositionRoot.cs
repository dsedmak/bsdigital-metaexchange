using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace BSDigital.MetaExchange.Core.Api.AspNetCore;

public static class CompositionRoot
{
    public static void MapAspNetCoreApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllers();
    }

    public static void AddAspNetCoreApi(this IServiceCollection services)
    {
        services.AddControllers().AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        
        /*
This concurrency limiter is crucial for the PlaceMarketOrder handler because:

The handler generates execution plans based on the current state of order books and exchange balances
If multiple requests were processed simultaneously, they would all see the same state and generate plans assuming the full available balance/orders
However, only one plan can actually succeed - the others would fail because the first successful execution would have already consumed the balance/orders
The concurrency limiter ensures requests are processed one at a time (PermitLimit = 1), maintaining a queue of up to 100 requests that are processed in order of arrival (OldestFirst).

The only way to avoid this limitation would be to batch multiple orders together into a single request, which would allow them to be planned together considering their combined impact on balances and order books.
I consider this outside the scope of this task by which I mean I tried several implementations that would have taken too long to properly implement.
We can discuss this further when we meet.
         */
        services.AddRateLimiter(options =>
            options.AddConcurrencyLimiter("onatatime", limiterOptions =>
            {
                limiterOptions.PermitLimit = 1;
                limiterOptions.QueueLimit = 100;
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            }));
    }
}