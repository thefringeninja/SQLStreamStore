namespace SqlStreamStore;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Shouldly;
using SqlStreamStore.Streams;
using Xunit;
using Xunit.Abstractions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

public class MigrationTests {
	private readonly SerilogLoggerFactory _loggerFactory;
	private readonly ILogger _logger;

	public MigrationTests(ITestOutputHelper testOutputHelper) {
		_loggerFactory = new SerilogLoggerFactory(new LoggerConfiguration()
			.MinimumLevel.Debug()
			.Enrich.FromLogContext()
			.WriteTo.TestOutput(testOutputHelper)
			.CreateLogger());
		_logger = _loggerFactory.CreateLogger<MigrationTests>();
	}

	[Fact]
	public async Task Can_migrate() {
		// Set up an old schema + data.
		var schema = "baz";
		using var v2Pool = new MsSqlStreamStoreFixturePool();
		using var v2Fixture = await v2Pool.Get(_loggerFactory, schema);
		var v2Store = v2Fixture.Store;
		await v2Store.AppendToStream("stream-1",
			ExpectedVersion.NoStream,
			AcceptanceTests.CreateNewStreamMessages(1, 2, 3));
		await v2Store.AppendToStream("stream-2",
			ExpectedVersion.NoStream,
			AcceptanceTests.CreateNewStreamMessages(1, 2, 3));

		await v2Store.SetStreamMetadata("stream-1", ExpectedVersion.Any, maxAge: 10, maxCount: 20);
		v2Store.Dispose();

		var settings = new MsSqlStreamStoreV3Settings(v2Fixture.ConnectionString) {
			Schema = schema,
		};

		using var v3Store = new MsSqlStreamStoreV3(settings);

		var checkSchemaResult = await v3Store.CheckSchema();
		checkSchemaResult.IsMatch().ShouldBeFalse();

		var progress = new Progress<MigrateProgress>();
		progress.ProgressChanged += (_, migrateProgress) =>
			_logger.LogInformation("Migration stage complete: {MigrateProgressStage}", migrateProgress.Stage);

		await v3Store.Migrate(progress, CancellationToken.None);

		checkSchemaResult = await v3Store.CheckSchema();
		checkSchemaResult.IsMatch().ShouldBeTrue();

		var listStreamsResult = await v3Store.ListStreams(Pattern.EndsWith("1"));
		listStreamsResult.StreamIds.Length.ShouldBe(2);
	}
}
