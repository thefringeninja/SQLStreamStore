namespace SqlStreamStore.Internal.HoneyBearHalClient.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

internal static class ResourceConverterExtensions {
	internal static T Data<T>(this IResource source)
		where T : class {
		var data = Activator.CreateInstance<T>();
		var dataType = typeof(T);

		foreach (var property in dataType.GetTypeInfo().DeclaredProperties) {
			var propertyName = property.Name;
			var attribute = property.GetCustomAttributes<JsonPropertyNameAttribute>().FirstOrDefault();
			if (!string.IsNullOrEmpty(attribute?.Name))
				propertyName = attribute.Name;

			var pair = source.FirstOrDefault(p => p.Key.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
			if (pair.Key == null)
				continue;

			var propertyType = property.PropertyType;

			property.SetValue(data,
				pair.Value switch {
					JsonObject complex => JsonSerializer.Deserialize(complex.ToString(), propertyType),
					JsonArray array => JsonSerializer.Deserialize(array.ToString(), propertyType),
					not null => TypeDescriptor.GetConverter(propertyType).ConvertFromString(pair.Value.ToString()!),
					_ => null
				},
				null);
		}

		return data;
	}

	public static IEnumerable<T> Data<T>(this IEnumerable<IResource<T>> source)
		where T : class, new()
		=> source.Select(s => s.Data);
}