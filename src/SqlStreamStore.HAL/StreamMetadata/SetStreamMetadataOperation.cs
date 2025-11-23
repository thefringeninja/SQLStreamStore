namespace SqlStreamStore.HAL.StreamMetadata;

using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

internal class SetStreamMetadataOperation : IStreamStoreOperation<Unit> {
	public static async Task<SetStreamMetadataOperation> Create(HttpContext context) {
		var node = await JsonNode.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted);

		if (node is not JsonObject body) {
			throw new InvalidOperationException();
		}

		return new SetStreamMetadataOperation(context, body);
	}

	private SetStreamMetadataOperation(HttpContext context, JsonObject body) {
		var request = context.Request;
		Path = request.Path;
		StreamId = context.GetRouteData().GetStreamId();
		ExpectedVersion = request.GetExpectedVersion();
		MaxAge = body["maxAge"]?.GetValue<int?>();
		MaxCount = body["maxCount"]?.GetValue<int?>();
		MetadataJson = body["metadataJson"]?.AsObject();
	}

	public PathString Path { get; }
	public string StreamId { get; }
	public int ExpectedVersion { get; }
	public JsonObject? MetadataJson { get; }
	public int? MaxCount { get; }
	public int? MaxAge { get; }

	public async Task<Unit> Invoke(IStreamStore streamStore, CancellationToken ct) {
		await streamStore.SetStreamMetadata(
			StreamId,
			ExpectedVersion,
			MaxAge,
			MaxCount,
			MetadataJson?.ToString(),
			ct);

		return Unit.Instance;
	}
}