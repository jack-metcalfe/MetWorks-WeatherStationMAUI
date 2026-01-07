namespace Utility;
public class PrivateInstanceJsonDeserializerHelper<T> : JsonConverter<T>
    where T : class
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Parse the JSON into a JsonElement for inspection
        var jsonElement = JsonDocument.ParseValue(ref reader).RootElement;

        // Attempt to find a non-public parameterless constructor
        var ctor = typeof(T).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        if (ctor is null)
            throw new InvalidOperationException(
                $"Type '{typeof(T).FullName}' must have a non-public parameterless constructor to support registry deserialization.");

        // Create the instance
        var instance = ctor.Invoke(null) as T
            ?? throw new InvalidOperationException($"Failed to instantiate type '{typeof(T).FullName}'.");

        // Hydrate properties manually or via a registry-aware helper
        HydrateProperties(instance, jsonElement, options);

        return instance;
    }
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        // Preserve runtime type fidelity
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
    private static void HydrateProperties(T instance, JsonElement jsonElement, JsonSerializerOptions options)
    {
        var propertyFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var properties = typeof(T).GetProperties(propertyFlags);

        foreach (var property in properties)
        {
            if (!property.CanWrite)
                continue;

            // Get the JSON property name from the attribute, if present
            var jsonName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;

            if (jsonElement.TryGetProperty(jsonName, out var propValue))
            {
                var deserialized = JsonSerializer.Deserialize(propValue.GetRawText(), property.PropertyType, options);
                property.SetValue(instance, deserialized);
            }
        }
    }
}
