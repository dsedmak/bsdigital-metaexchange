namespace BSDigital.MetaExchange.Framework.UseCases;

public interface IEvent
{
}

public interface IEventHandler<TEvent>
    where TEvent : IEvent
{
    public ValueTask Handle(TEvent @event);
}

public delegate ValueTask EventHandler<TEvent>(TEvent @event)
    where TEvent : IEvent;