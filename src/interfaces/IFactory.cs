namespace MetWorks.Interfaces;
public interface IFactory<TReturnType, TInputType>
{
    TReturnType Create(ILogger iFileLogger, TInputType input);
}
