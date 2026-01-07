namespace InterfaceDefinition;
public interface IValidator<TReturn, TInput>
{
    Task<TReturn> ValidateAsync(TInput input);
}
