namespace SqlStreamStore.Internal.HoneyBearHalClient.Serialization;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using SqlStreamStore.Internal.HoneyBearHalClient.Models;

internal static class HalResourceJsonReader {
	public static async Task<IResource> ReadResource(
		Stream reader,
		CancellationToken cancellationToken = default)
		=> new JResource((await JsonNode.ParseAsync(reader, cancellationToken: cancellationToken))!.AsObject());

	private class JResource : IResource {
		private static readonly Uri s_baseAddress = new UriBuilder().Uri;
		private readonly JsonObject _inner;

		public string? Rel { get; set; }

		public string? Href {
			get => _inner["href"]?.GetValue<string?>();
			set => _inner["href"] = value;
		}

		public string? Name {
			get => _inner["name"]?.GetValue<string>();
			set => _inner["name"] = value;
		}

		public JResource(JsonObject inner, string? rel = null) {
			ArgumentNullException.ThrowIfNull(inner);
			_inner = inner;
			Rel = rel;
			BaseAddress = s_baseAddress;
		}

		public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
			=> _inner
				.Select(x => new KeyValuePair<string, object?>(
					x.Key,
					x.Value))
				.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public IList<ILink> Links => GetLinks().ToArray();
		public IList<IResource> Embedded => GetEmbedded().ToArray();

		public Uri BaseAddress { get; private set; }

		public IResource WithBaseAddress(Uri baseAddress)
			=> new JResource(_inner, Rel) {
				BaseAddress = baseAddress
			};

		private IEnumerable<ILink> GetLinks() {
			var links = _inner["_links"]?.AsObject() ?? [];

			foreach (var property in links) {
				switch (property.Value) {
					case JsonArray array: {
						foreach (var link in array.OfType<JsonObject>()) {
							yield return new JLink(link, property.Key);
						}

						break;
					}
					case JsonObject o:
						yield return new JLink(o, property.Key);
						break;
				}
			}
		}

		private IEnumerable<IResource> GetEmbedded() {
			var embedded = _inner["_embedded"]?.AsObject() ?? [];

			foreach (var property in embedded) {
				switch (property.Value) {
					case JsonArray array: {
						foreach (var resource in array.OfType<JsonObject>()) {
							yield return new JResource(resource, property.Key).WithBaseAddress(BaseAddress);
						}

						break;
					}
					case JsonObject o:
						yield return new JResource(o, property.Key).WithBaseAddress(BaseAddress);
						break;
				}
			}
		}
	}

	private class JLink : ILink {
		private readonly JsonObject _inner;
		public string? Rel { get; set; }

		public string? Href {
			get => _inner["href"]?.GetValue<string>();
			set => _inner["href"] = value;
		}

		public string? Name {
			get => _inner["name"]?.GetValue<string>();
			set => _inner["name"] = value;
		}

		public bool Templated {
			get => _inner["templated"]?.GetValue<bool>() ?? false;
			set => _inner["templated"] = value;
		}

		public string? Title {
			get => _inner["title"]?.GetValue<string>();
			set => _inner["title"] = value;
		}

		public JLink(JsonObject inner, string rel) {
			ArgumentNullException.ThrowIfNull(inner);
			_inner = inner;
			Rel = rel;
		}
	}
}