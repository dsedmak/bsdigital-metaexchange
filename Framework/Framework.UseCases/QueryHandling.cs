

namespace BSDigital.MetaExchange.Framework.UseCases;

public interface IQuery<TResult>
{
}

public interface IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public ValueTask<TResult> Handle(TQuery query);
}

public delegate ValueTask<TResult> QueryHandler<TQuery, TResult>(TQuery query)
    where TQuery : IQuery<TResult>;
