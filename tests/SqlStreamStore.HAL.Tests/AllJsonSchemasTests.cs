namespace SqlStreamStore.HAL.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

public class AllJsonSchemasTests {
	private static Regex s_isSqlStreamStoreSchema = new(@"Schema\.(.*)\.schema\.json$");

	private static byte[] s_bom =
	{
		0xEF, 0xBB, 0xBF
	};

	private static StreamReader GetStreamReader(string manifestName) => new(GetStream(manifestName));

	private static Stream GetStream(string manifestName)
		=> typeof(SchemaSet<>)
			.GetTypeInfo()
			.Assembly
			.GetManifestResourceStream(manifestName) ?? throw new Exception($"Manifest {manifestName} not found. BUG!!1");

	public static IEnumerable<object[]> GetJsonSchemas() => from manifestName in typeof(SchemaSet<>)
			.GetTypeInfo()
			.Assembly
			.GetManifestResourceNames()
		where s_isSqlStreamStoreSchema.IsMatch(manifestName)
		select new[] { manifestName };


	[Theory, MemberData(nameof(GetJsonSchemas))]
	public async Task byte_order_mark_not_present(string manifestName) {
		byte[] firstThreeBytes = new byte[3];

		using (var stream = GetStream(manifestName)) {
			await stream.ReadExactlyAsync(firstThreeBytes);

			Enumerable.SequenceEqual(firstThreeBytes, s_bom)
				.ShouldBeFalse();
		}
	}

	[Theory, MemberData(nameof(GetJsonSchemas))]
	public void json_schema_is_compatible_with_markdown_generator(string manifestName) {
		using (var reader = GetStreamReader(manifestName)) {
			JsonSerializer.Deserialize<JsonObject>(reader.ReadToEnd())!["$schema"]?.GetValue<string>()
				.ShouldBe("http://json-schema.org/draft-07/schema#");
		}
	}
}
