namespace SqlStreamStore.HAL.Index;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;

internal class IndexResource : IResource {
	public SchemaSet? Schema { get; }

	private readonly object _data;

	public IndexResource(IStreamStore streamStore, Assembly? serverAssembly) {
		var streamStoreType = streamStore.GetType();
		var streamStoreTypeName = streamStoreType.Name;
		var versions = new Dictionary<string, string> {
			["streamStore"] = GetVersion(streamStoreType),
			[serverAssembly?.GetName().Name?.Split('.').LastOrDefault() ?? "Server"] =
				GetVersion(serverAssembly)
		};

		_data = new {
			provider = streamStoreTypeName.Substring(0, streamStoreTypeName.Length - "StreamStore".Length),
			versions
		};
	}

	private static string GetVersion(Type type) => GetVersion(type.Assembly);

	private static string GetVersion(Assembly? assembly)
		=> assembly
			   ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
			   ?.InformationalVersion
		   ?? assembly
			   ?.GetCustomAttribute<AssemblyVersionAttribute>()
			   ?.Version
		   ?? "unknown";

	public Response Get() => new HalJsonResponse(new HALResponse(_data)
		.AddLinks(
			Links
				.FromPath(PathString.Empty)
				.Index().Self()
				.Find()
				.Browse()
				.Add(Constants.Relations.Feed, LinkFormatter.AllStream())));

	public override string? ToString() => _data.ToString();
}