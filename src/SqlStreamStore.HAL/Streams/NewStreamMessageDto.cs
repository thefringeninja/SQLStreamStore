namespace SqlStreamStore.HAL.Streams;

using System;
using System.Text.Json.Nodes;
using SqlStreamStore.Streams;

internal class NewStreamMessageDto {
	public required Guid MessageId { get; set; }
	public required string Type { get; set; }
	public required JsonObject JsonData { get; set; }
	public JsonObject? JsonMetadata { get; set; }

	public NewStreamMessage ToNewStreamMessage() => new(MessageId, Type, JsonData.ToString(), JsonMetadata?.ToString());
}
