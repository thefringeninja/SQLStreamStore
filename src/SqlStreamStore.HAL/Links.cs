namespace SqlStreamStore.HAL;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Hallo;
using Microsoft.AspNetCore.Http;

internal class Links {
	private readonly PathString _path;
	private readonly List<(string rel, string href, string? title)> _links;
	private readonly string _relativePathToRoot;

	public static Links FromOperation<T>(IStreamStoreOperation<T> operation) => FromPath(operation.Path);

	public static Links FromPath(PathString path) => new(path);

	public static Links FromRequestMessage(HttpRequestMessage requestMessage)
		=> FromPath(new PathString(requestMessage.RequestUri?.AbsolutePath));

	private Links(PathString path) {
		_path = path;
		var segmentCount = path.Value?.Split('/').Length ?? 0;
		_relativePathToRoot =
			segmentCount < 2
				? "./"
				: string.Join(string.Empty, Enumerable.Repeat("../", segmentCount - 2));

		_links = [];
	}

	public Links Add(string rel, string href, string? title = null) {
		ArgumentNullException.ThrowIfNull(rel);
		ArgumentNullException.ThrowIfNull(href);

		_links.Add((rel, href, title));

		return this;
	}

	public Links Self() {
		var (_, href, title) = _links[_links.Count - 1];

		return Add(Constants.Relations.Self, href, title);
	}

	public Links AddSelf(string rel, string href, string? title = null)
		=> Add(rel, href, title)
			.Add(Constants.Relations.Self, href, title);

	public Link[] ToHalLinks() {
		var links = new Link[_links.Count + 1];

		for (var i = 0; i < _links.Count; i++) {
			var (rel, href, title) = _links[i];

			links[i] = new Link(rel, Resolve(href), Constants.MediaTypes.HalJson, title: title);
		}

		links[_links.Count] = new Link(
			Constants.Relations.Curies,
			Resolve(LinkFormatter.DocsTemplate()),
			title: "Documentation",
			name: Constants.Relations.StreamStorePrefix,
			hrefLang: "en",
			type: Constants.MediaTypes.TextMarkdown);

		return links;
	}

	private string Resolve(string relativeUrl) => $"{_relativePathToRoot}{relativeUrl}" switch {
		null or "" => "./",
		{ } s => s
	};

	public static implicit operator Link[](Links links) => links.ToHalLinks();

	private static string FormatLink(
		string baseAddress,
		string direction,
		int maxCount,
		long position,
		bool prefetch)
		=> $"{baseAddress}?d={direction}&m={maxCount}&p={position}&e={(prefetch ? 1 : 0)}";

	[Obsolete("", true)]
	public static string FormatBackwardLink(string baseAddress, int maxCount, long position, bool prefetch)
		=> FormatLink(baseAddress, "b", maxCount, position, prefetch);
}
