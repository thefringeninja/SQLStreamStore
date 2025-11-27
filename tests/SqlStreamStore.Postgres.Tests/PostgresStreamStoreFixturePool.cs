namespace SqlStreamStore;

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SqlStreamStore.TestUtils.Postgres;
using Xunit;

public class PostgresStreamStoreV3FixturePool : IAsyncLifetime {
	private readonly ConcurrentDictionary<string, ConcurrentQueue<PostgresStreamStoreFixture>> _fixturePoolBySchema =
		new();

	public async Task<PostgresStreamStoreFixture> Get(
		ILoggerFactory loggerFactory,
		string schema = "dbo") {
		var logger = loggerFactory.CreateLogger<PostgresStreamStoreV3FixturePool>();

		var fixturePool = _fixturePoolBySchema.GetOrAdd(
			schema,
			_ => new ConcurrentQueue<PostgresStreamStoreFixture>());

		if (!fixturePool.TryDequeue(out var fixture)) {
			var databaseName = $"test_{Guid.NewGuid():n}";
			var dockerInstance = new PostgresContainer(databaseName);
			await dockerInstance.Start();
			await dockerInstance.CreateDatabase();

			fixture = new PostgresStreamStoreFixture(
				schema,
				dockerInstance,
				databaseName,
				onDispose: () => fixturePool.Enqueue(fixture!),
				loggerFactory);

			logger.LogInformation("Using new fixture with db {DatabaseName}", databaseName);
		} else {
			logger.LogInformation("Using pooled fixture with db {FixtureDatabaseName}", fixture.DatabaseName);
		}

		await fixture.Prepare();

		return fixture;
	}

	public Task InitializeAsync() {
		return Task.CompletedTask;
	}

	public Task DisposeAsync() {
		return Task.CompletedTask;
	}
}
