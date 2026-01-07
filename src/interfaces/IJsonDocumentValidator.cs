namespace InterfaceDefinition;
public interface IJsonDocumentValidator
{
    Task<IValidationResult> ValidateAsync(ReadOnlyMemory<char> readOnlyMemoryJsonDocument);
    Task<IValidationResult> ValidateAsync(JsonDocument jsonDocument);
}
