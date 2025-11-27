namespace SqlStreamStore.HAL.Tests;

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shouldly;
using SqlStreamStore.Streams;
using Xunit;
using Xunit.Abstractions;

public class StreamMetadataTests {
	private const string StreamId = "a-stream";

	private readonly SqlStreamStoreHalMiddlewareFixture _fixture;

	public StreamMetadataTests(ITestOutputHelper output) {
		_fixture = new SqlStreamStoreHalMiddlewareFixture(output);
	}

	[Fact]
	public async Task get_metadata_when_metadata_stream_does_not_exist() {
		using (var response =
		       await _fixture.HttpClient.SendAsync(
			       new HttpRequestMessage(HttpMethod.Get, $"/{Constants.Paths.Streams}/{StreamId}/metadata"))) {
			response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
			response.Headers.ETag.ShouldBe(new EntityTagHeaderValue($@"""{ExpectedVersion.EmptyStream}"""));

			var resource = await response.AsHal();

			JsonNode.DeepEquals(
				JsonNode.Parse($$"""
				                 { 
				                  "streamId": "{{StreamId}}",
				                  "metadataStreamVersion": -1,
				                  "maxAge": null,
				                  "maxCount": null,
				                  "metadataJson": null
				                 }
				                 """),
				resource.State).ShouldBe(true);
			resource.ShouldLink(
				Links
					.FromRequestMessage(response.RequestMessage!)
					.Index()
					.Find()
					.Add(Constants.Relations.Metadata, $"streams/{StreamId}/metadata").Self()
					.Add(Constants.Relations.Feed, $"streams/{StreamId}", StreamId));
		}
	}

	[Fact]
	public async Task get_metadata_when_metadata_stream_does_exist() {
		using (await _fixture.HttpClient.SendAsync(
			       new HttpRequestMessage(HttpMethod.Post, $"/{Constants.Paths.Streams}/{StreamId}/metadata") {
				       Content = JsonContent.Create(new {
					       maxAge = 30,
					       maxCount = 20,
					       metadataJson = new {
						       type = "a-type"
					       }
				       })
			       }))
		using (var response =
		       await _fixture.HttpClient.SendAsync(
			       new HttpRequestMessage(HttpMethod.Get, $"/{Constants.Paths.Streams}/{StreamId}/metadata"))) {
			response.StatusCode.ShouldBe(HttpStatusCode.OK);
			response.Headers.ETag.ShouldBe(new EntityTagHeaderValue(@"""0"""));

			var resource = await response.AsHal();

			JsonNode.DeepEquals(
				resource.State,
				JsonNode.Parse($$"""
				                 { 
				                  "streamId": "{{StreamId}}",
				                  "metadataStreamVersion": 0,
				                  "maxAge": 30,
				                  "maxCount": 20,
				                  "metadataJson": { "type":  "a-type" }
				                 }
				                 """));

			resource.ShouldLink(
				Links
					.FromRequestMessage(response.RequestMessage!)
					.Index()
					.Find()
					.Add(Constants.Relations.Metadata, $"streams/{StreamId}/metadata").Self()
					.Add(Constants.Relations.Feed, $"streams/{StreamId}", StreamId));
		}
	}

	[Fact]
	public async Task set_metadata() {
		using (var response = await _fixture.HttpClient.SendAsync(
			       new HttpRequestMessage(HttpMethod.Post, $"/{Constants.Paths.Streams}/{StreamId}/metadata") {
				       Content = new StringContent(JObject.FromObject(new {
					       maxAge = 30,
					       maxCount = 20,
					       metadataJson = new {
						       type = "a-type"
					       }
				       }).ToString()) {
					       Headers =
					       {
						       ContentType = new MediaTypeHeaderValue("application/json")
					       }
				       }
			       })) {
			response.StatusCode.ShouldBe(HttpStatusCode.OK);

			var resource = await response.AsHal();

			JsonNode.DeepEquals(
				resource.State,
				JsonNode.Parse($$"""
				                 { 
				                  "streamId": "{{StreamId}}",
				                  "maxAge": 30,
				                  "maxCount": 20,
				                  "metadataJson": { "type":  "a-type" }
				                 }
				                 """)).ShouldBeTrue();

			resource.ShouldLink(
				Links
					.FromRequestMessage(response.RequestMessage!)
					.Index()
					.Find()
					.Add(Constants.Relations.Metadata, $"streams/{StreamId}/metadata").Self()
					.Add(Constants.Relations.Feed, $"streams/{StreamId}", StreamId));
		}
	}

	private static IEnumerable<int[]> WrongExpectedVersions() {
		yield return new[] { ExpectedVersion.NoStream, ExpectedVersion.NoStream };
		yield return new[] { ExpectedVersion.NoStream, 2 };
	}

	public static IEnumerable<object[]> WrongExpectedVersionCases()
		=> WrongExpectedVersions().Select(s => new object[] { s });

	[Theory]
	[MemberData(nameof(WrongExpectedVersionCases))]
	public async Task set_wrong_expected_version(int[] expectedVersions) {
		for (var i = 0; i < expectedVersions.Length - 1; i++) {
			using (await _fixture.HttpClient.SendAsync(
				       new HttpRequestMessage(HttpMethod.Post, $"/{Constants.Paths.Streams}/{StreamId}/metadata") {
					       Headers =
					       {
						       { Constants.Headers.ExpectedVersion, $"{expectedVersions[i]}" }
					       },
					       Content = new StringContent(JObject.FromObject(new {
						       maxAge = 30,
						       maxCount = 20,
						       metadataJson = new {
							       type = "a-type"
						       }
					       }).ToString()) {
						       Headers =
						       {
							       ContentType = new MediaTypeHeaderValue("application/json")
						       }
					       }
				       })) { }
		}

		using (var response = await _fixture.HttpClient.SendAsync(
			       new HttpRequestMessage(HttpMethod.Post, $"/{Constants.Paths.Streams}/{StreamId}/metadata") {
				       Headers =
				       {
					       { Constants.Headers.ExpectedVersion, $"{expectedVersions[expectedVersions.Length - 1]}" }
				       },
				       Content = new StringContent(JObject.FromObject(new {
					       maxAge = 30,
					       maxCount = 20,
					       metadataJson = new {
						       type = "another-type"
					       }
				       }).ToString()) {
					       Headers =
					       {
						       ContentType = new MediaTypeHeaderValue("application/json")
					       }
				       }
			       })) {
			response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
			response.Content.Headers.ContentType.ShouldBe(new MediaTypeHeaderValue(
				Constants.MediaTypes.HalJson));
		}

		var page = await _fixture.StreamStore.ReadStreamForwards($"$${StreamId}", 0, int.MaxValue);

		page.Status.ShouldBe(PageReadStatus.Success);
		page.Messages.Length.ShouldBe(expectedVersions.Length - 1);
	}
}