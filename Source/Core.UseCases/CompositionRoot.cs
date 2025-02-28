using BSDigital.Injectable;
using Microsoft.Extensions.DependencyInjection;

namespace BSDigital.MetaExchange.Core.UseCases;

public static class CompositionRoot
{
    public static void AddUseCases(this IServiceCollection services)
    {
        services.RegisterInjectableTypes(typeof(CompositionRoot).Assembly);
    }
}