namespace SqlStreamStore.HAL.Tests;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

internal static class HttpResponseMessageExtensions {
	public static async Task<Resource> AsHal(this HttpResponseMessage response)
		=> ParseResource(JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject());

	/// <summary>
	/// https://github.com/wis3guy/HalClient.Net/blob/master/HalClient.Net/Parser/HalJsonParser.cs
	/// </summary>
	/// <param name="outer"></param>
	/// <returns></returns>
	private static Resource ParseResource(JsonObject outer) {
		var links = new List<Link>();
		var embedded = new List<(string, Resource)>();
		var state = JsonNode.Parse("{}")!.AsObject();

		foreach (var (propetyName, inner) in outer) {
			if (inner is JsonObject o) {
				switch (propetyName) {
					case "_links":
						links.AddRange(ParseObjectOrArrayOfObjects(o, ParseLink));
						break;
					case "_embedded":
						embedded.AddRange(ParseObjectOrArrayOfObjects(o, ParseEmbeddedResource));
						break;
					default:
						state.Add(propetyName, o.DeepClone());
						break;
				}
			} else {
				switch (propetyName) {
					case "_links":
					case "_embedded":
						if (inner is not null) {
							throw new FormatException(
								$"Invalid value for {propetyName}: {inner}");
						}
						break;
					default:
						state.Add(propetyName, inner?.DeepClone());
						break;
				}
			}
		}

		return new Resource(state, links.ToArray(), embedded.ToArray());
	}

	private static (string, Resource) ParseEmbeddedResource(JsonObject outer, string rel)
		=> (rel, ParseResource(outer));

	private static Link ParseLink(JsonObject outer, string rel) {
		var link = new Link { Rel = rel };

		string? href = null;

		foreach (var (propertyName, inner) in outer) {
			if (inner is null) {
				continue;
			}

			switch (propertyName) {
				case "href":
					href = inner.GetValue<string>();
					break;
				case "templated":

					//link.Templated = value.Equals("true", StringComparison.OrdinalIgnoreCase);
					break;
				case "type":
					//link.Type = value;
					break;
				case "deprication":
					//link.SetDeprecation(value);
					break;
				case "name":
					//link.Name = value;
					break;
				case "profile":
					//link.SetProfile(value);
					break;
				case "title":
					link.Title = inner.GetValue<string>();
					break;
				case "hreflang":
					//link.HrefLang = value;
					break;
				default:
					throw new NotSupportedException("Unsupported link attribute encountered: " + propertyName);
			}
		}

		link.Href = href;

		return link;
	}

	private static IEnumerable<T> ParseObjectOrArrayOfObjects<T>(JsonObject outer, Func<JsonObject, string, T> factory) {
		foreach (var (rel, inner) in outer) {
			if (inner is JsonArray array) {
				foreach (var child in array) {
					if (child is JsonObject childObject)
						yield return factory(childObject, rel);
				}
			} else if (inner is JsonObject o)
				yield return factory(o, rel);
		}
	}
}