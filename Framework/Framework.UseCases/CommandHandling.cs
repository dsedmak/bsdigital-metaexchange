

namespace BSDigital.MetaExchange.Framework.UseCases;

public interface ICommand<TResult>
{
}

public interface ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public ValueTask<TResult> Handle(TCommand command);
}

public delegate ValueTask<TResult> CommandHandler<TCommand, TResult>(TCommand command)
    where TCommand : ICommand<TResult>;
