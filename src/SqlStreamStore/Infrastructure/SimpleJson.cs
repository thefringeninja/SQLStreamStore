namespace SqlStreamStore.Infrastructure;

using System.Text.Json;
using System.Text.Json.Serialization;

public static class SimpleJson {
	private static readonly JsonSerializerOptions SimpleJsonOptions = new() {
		IncludeFields = true,
		PropertyNameCaseInsensitive = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.Never, // Include nulls, the default for SimpleJson
		PropertyNamingPolicy = null, // Use PascalCase matching .NET property names
		WriteIndented = false // Minified JSON by default, like SimpleJson
	};


	public static string SerializeObject<T>(T value) => JsonSerializer.Serialize(value, SimpleJsonOptions);

	public static T? DeserializeObject<T>(string? value) =>
		value is null ? default : JsonSerializer.Deserialize<T>(value, SimpleJsonOptions);
}
