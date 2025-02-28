using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BSDigital.Injectable;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public abstract class InjectableAttribute : Attribute
{
    private protected InjectableAttribute(Type providedType, Type requestedType, ServiceLifetime serviceLifetime)
    {
        ProvidedType = providedType;
        RequestedType = requestedType;
        ServiceLifetime = serviceLifetime;
    }

    public Type ProvidedType { get; }
    public Type RequestedType { get; }
    public ServiceLifetime ServiceLifetime { get; }

    public void Register(IServiceCollection services)
    {
        var serviceDescriptor = new ServiceDescriptor(RequestedType, ProvidedType, ServiceLifetime);
        services.TryAdd(serviceDescriptor);
    }
}

#if NET7_0_OR_GREATER

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class SingletonAttribute<TImplementation, TInterface> : InjectableAttribute
{
    public SingletonAttribute() : base(typeof(TImplementation), typeof(TInterface), ServiceLifetime.Singleton)
    {
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ScopedAttribute<TImplementation, TInterface> : InjectableAttribute
{
    public ScopedAttribute() : base(typeof(TImplementation), typeof(TInterface), ServiceLifetime.Scoped)
    {
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class TransientAttribute<TImplementation, TInterface> : InjectableAttribute
{
    public TransientAttribute() : base(typeof(TImplementation), typeof(TInterface), ServiceLifetime.Transient)
    {
    }
}

#endif

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class SingletonAttribute : InjectableAttribute
{
    public SingletonAttribute(Type providedType, Type requestedType) : base(providedType, requestedType, ServiceLifetime.Singleton)
    {
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ScopedAttribute : InjectableAttribute
{
    public ScopedAttribute(Type providedType, Type requestedType) : base(providedType, requestedType, ServiceLifetime.Scoped)
    {
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class TransientAttribute : InjectableAttribute
{
    public TransientAttribute(Type providedType, Type requestedType) : base(providedType, requestedType, ServiceLifetime.Transient)
    {
    }
}