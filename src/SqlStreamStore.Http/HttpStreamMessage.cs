namespace SqlStreamStore;

using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using SqlStreamStore.Streams;

internal class HttpStreamMessage {
	public required Guid MessageId { get; set; }
	public required DateTimeOffset CreatedUtc { get; set; }
	public required long Position { get; set; }
	public required string StreamId { get; set; }
	public required int StreamVersion { get; set; }
	public required string Type { get; set; }
	public JsonObject? Payload { get; set; }
	public JsonObject? Metadata { get; set; }

	public StreamMessage ToStreamMessage(Func<CancellationToken, Task<string?>> getPayload)
		=> new(
			StreamId,
			MessageId,
			StreamVersion,
			Position,
			CreatedUtc.DateTime,
			Type,
			Metadata?.ToString(),
			getPayload);
}
