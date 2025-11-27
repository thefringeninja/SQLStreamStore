namespace SqlStreamStore;

using System;
using SqlStreamStore.Streams;

internal class PostgresNewStreamMessage {
	public required Guid MessageId { get; set; }
	public required string JsonData { get; set; }
	public string? JsonMetadata { get; set; }
	public required string Type { get; set; }

	public static PostgresNewStreamMessage FromNewStreamMessage(NewStreamMessage message)
		=> new() {
			MessageId = message.MessageId,
			Type = message.Type,
			JsonData = message.JsonData,
			JsonMetadata = string.IsNullOrEmpty(message.JsonMetadata) ? null : message.JsonMetadata,
		};
}
