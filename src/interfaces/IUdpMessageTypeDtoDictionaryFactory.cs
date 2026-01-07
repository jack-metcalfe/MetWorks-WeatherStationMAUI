namespace InterfaceDefinition;
public interface IUdpMessageTypeDtoDictionaryFactory : IFactory<
    IUdpMessageTypeDtoDictionary,
    (
        ReadOnlyMemory<char> messageTypeSchemaReadOnlyMemoryOfChar,
        ReadOnlyMemory<char> messageTypeDefinitionsReadOnlyMemoryOfChar
    )
>
{
}
