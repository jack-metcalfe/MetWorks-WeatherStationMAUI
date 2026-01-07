namespace InterfaceDefinition;
public interface IReadOnlyDictionarySimple<TInput, TResult>
{
    TResult GetValue(TInput key);
    bool TryGetValue(TInput key, out TResult value);
}
