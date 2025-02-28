using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace BSDigital.Injectable;

public static class RegistrationHelper
{
    public static void RegisterInjectableTypes(this IServiceCollection services, Assembly assembly)
    {
        foreach (var type in assembly.GetTypes().Where(t => t.IsDefined(typeof(InjectableAttribute))))
        {
            var injectableAttributes = type.GetCustomAttributes<InjectableAttribute>();
            foreach (var injectableAttribute in injectableAttributes)
            {
                injectableAttribute.Register(services);
            }
        }
    }
}