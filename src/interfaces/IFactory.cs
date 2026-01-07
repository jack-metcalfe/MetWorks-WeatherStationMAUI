namespace InterfaceDefinition;
public interface IFactory<TReturnType, TInputType>
{
    TReturnType Create(IFileLogger iFileLogger, TInputType input);
}
