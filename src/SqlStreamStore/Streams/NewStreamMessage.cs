namespace SqlStreamStore.Streams;

using System;

/// <summary>
///     Represents a message to be appended to a stream.
/// </summary>
public class NewStreamMessage {
	/// <summary>
	///     The message data serialized as JSON.
	/// </summary>
	public readonly string JsonData;

	/// <summary>
	///     The id of the messaage.
	/// </summary>
	public readonly Guid MessageId;

	/// <summary>
	///     The message type. Note: it is not recommended to use CLR type names
	///     (fully qualified or not).
	/// </summary>
	public readonly string Type;

	/// <summary>
	///     Metadata serialzied as JSON.
	/// </summary>
	public readonly string JsonMetadata;

	/// <summary>
	///     Initializes a new instance of <see cref="NewStreamMessage"/>.
	/// </summary>
	/// <param name="messageId">The id of the messaage.</param>
	/// <param name="type">
	///     The message type. Note: it is not recommended to use CLR type names
	///     (fully qualified or not).
	/// </param>
	/// <param name="jsonData">The message data serialized as JSON.</param>
	/// <param name="jsonMetadata"> Metadata serialzied as JSON.</param>
	public NewStreamMessage(Guid messageId, string type, string jsonData, string? jsonMetadata = null) {
		ArgumentOutOfRangeException.ThrowIfEqual(messageId, Guid.Empty);
		ArgumentException.ThrowIfNullOrWhiteSpace(type);
		ArgumentException.ThrowIfNullOrWhiteSpace(jsonData);

		MessageId = messageId;
		Type = type;
		JsonData = jsonData;
		JsonMetadata = jsonMetadata ?? string.Empty;
	}
}