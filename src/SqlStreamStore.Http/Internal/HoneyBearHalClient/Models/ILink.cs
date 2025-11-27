namespace SqlStreamStore.Internal.HoneyBearHalClient.Models;

using System.Text.Json.Serialization;

internal interface ILink : INode {
	[JsonPropertyName("templated")]
	bool Templated { get; set; }

	[JsonPropertyName("title")]
	string? Title { get; set; }
}