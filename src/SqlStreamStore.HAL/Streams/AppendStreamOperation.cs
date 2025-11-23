namespace SqlStreamStore.HAL.Streams;

using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SqlStreamStore.Streams;

internal class AppendStreamOperation : IStreamStoreOperation<AppendResult> {
	public static async Task<AppendStreamOperation> Create(HttpContext context) {
		var request = context.Request;
		var body = await JsonNode.ParseAsync(request.Body, cancellationToken: context.RequestAborted);

		return body switch {
			JsonArray json => new AppendStreamOperation(request.HttpContext, json),
			JsonObject json => new AppendStreamOperation(json, request.HttpContext),
			_ => throw new InvalidAppendRequestException("Invalid json detected.")
		};
	}

	private AppendStreamOperation(HttpContext context) {
		StreamId = context.GetRouteData().GetStreamId();
		ExpectedVersion = context.Request.GetExpectedVersion();
		NewStreamMessages = [];
	}

	private AppendStreamOperation(HttpContext context, JsonArray body)
		: this(context) {
		Path = context.Request.Path;
		NewStreamMessages = body.Select(ParseNewStreamMessage).ToArray();
	}

	private AppendStreamOperation(JsonObject body, HttpContext context)
		: this(context, [body]) { }

	private static NewStreamMessageDto ParseNewStreamMessage(JsonNode? newStreamMessage, int index) {
		if (!Guid.TryParse(newStreamMessage!["messageId"]?.GetValue<string>(), out var messageId)) {
			throw new InvalidAppendRequestException(
				$"'{nameof(messageId)}' at index {index} was improperly formatted.");
		}

		if (messageId == Guid.Empty) {
			throw new InvalidAppendRequestException($"'{nameof(messageId)}' at index {index} was empty.");
		}

		var type = newStreamMessage["type"]?.GetValue<string>();

		if (type == null) {
			throw new InvalidAppendRequestException($"'{nameof(type)}' at index {index} was not set.");
		}

		return new NewStreamMessageDto {
			MessageId = messageId,
			Type = type,
			JsonData = newStreamMessage["jsonData"]!.AsObject(),
			JsonMetadata = newStreamMessage["jsonMetadata"]?.AsObject()
		};
	}

	public string StreamId { get; }
	public int ExpectedVersion { get; }
	public NewStreamMessageDto[] NewStreamMessages { get; }
	public PathString Path { get; }

	public Task<AppendResult> Invoke(IStreamStore streamStore, CancellationToken ct)
		=> streamStore.AppendToStream(
			StreamId,
			ExpectedVersion,
			Array.ConvertAll(NewStreamMessages, dto => dto.ToNewStreamMessage()),
			ct);
}