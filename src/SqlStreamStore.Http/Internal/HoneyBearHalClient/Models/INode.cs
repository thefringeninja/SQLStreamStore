namespace SqlStreamStore.Internal.HoneyBearHalClient.Models;

using System.Text.Json.Serialization;

internal interface INode {
	[JsonIgnore]
	string? Rel { get; set; }

	[JsonPropertyName("href")]
	string? Href { get; set; }

	[JsonPropertyName("name")]
	string? Name { get; set; }
}