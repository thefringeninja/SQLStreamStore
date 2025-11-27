namespace SqlStreamStore.HAL;

using System.Collections.Generic;
using System.Collections.Immutable;
using Hallo;

internal class HALResponse(object? state, ImmutableDictionary<string, object>? embedded = null, Link[]? links = null)
	: HalRepresentation(state ?? new object(), embedded, links ?? []) {
	public HALResponse AddLinks(Link[] l) => new(state, embedded, [.. l, .. links ?? []]);

	public HALResponse AddEmbeddedCollection(string rel, IEnumerable<HALResponse> resources) =>
		new(state,
			embedded: (embedded ?? ImmutableDictionary<string, object>.Empty).Add(rel, resources),
			links);

	public HALResponse AddEmbeddedResource(string rel, HALResponse resource) =>
		new(state,
			embedded: (embedded ?? ImmutableDictionary<string, object>.Empty).Add(rel, resource),
			links);
}
