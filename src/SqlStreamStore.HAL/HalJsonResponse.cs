namespace SqlStreamStore.HAL;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Hallo;
using Hallo.Serialization;
using Microsoft.AspNetCore.Http;

internal class HalJsonResponse : Response {
	private readonly HalRepresentation _body;

	protected static readonly JsonSerializerOptions HalJsonSerializerOptions = new() {
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
		Converters =
		{
			new LinksConverter(),
			new HalRepresentationConverter(),
			new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
		},
	};

	public HalJsonResponse(HalRepresentation body, int statusCode = 200)
		: base(statusCode, Constants.MediaTypes.HalJson) {
		_body = body;
	}

	public override async Task WriteBody(HttpResponse response, CancellationToken cancellationToken) {
		await JsonSerializer.SerializeAsync(response.Body, _body, HalJsonSerializerOptions, cancellationToken);
	}
}