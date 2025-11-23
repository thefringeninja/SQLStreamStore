namespace SqlStreamStore.HAL;

using System.Text.Json.Nodes;

internal class StreamMessageHALResponse : HALResponse {
	public StreamMessageHALResponse(SqlStreamStore.Streams.StreamMessage message, string? payload)
		: base(new {
			message.MessageId,
			message.CreatedUtc,
			message.Position,
			message.StreamId,
			message.StreamVersion,
			message.Type,
			payload = FromString(payload),
			metadata = FromString(message.JsonMetadata)
		}) { }

	private static JsonNode? FromString(string? data) => string.IsNullOrEmpty(data) ? null : JsonNode.Parse(data);
}