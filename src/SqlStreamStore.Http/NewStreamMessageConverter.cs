namespace SqlStreamStore;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SqlStreamStore.Streams;

internal class NewStreamMessageConverter : JsonConverter<NewStreamMessage> {
	public override NewStreamMessage Read(
		ref Utf8JsonReader reader,
		Type typeToConvert,
		JsonSerializerOptions options) =>
		throw new NotImplementedException();

	public override void Write(Utf8JsonWriter writer, NewStreamMessage value, JsonSerializerOptions options) {
		writer.WriteStartObject();

		if (!string.IsNullOrEmpty(value.JsonMetadata)) {
			writer.WritePropertyName("jsonMetadata");
			writer.WriteRawValue(value.JsonMetadata);
		}

		writer.WriteString("messageId", value.MessageId);

		writer.WriteString("type", value.Type);

		writer.WritePropertyName("jsonData");
		writer.WriteRawValue(value.JsonData);


		writer.WriteEndObject();
	}
}