namespace SqlStreamStore;

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SqlStreamStore.TestUtils.MySql;
using Xunit;

public class MySqlStreamStoreFixturePool : IAsyncLifetime {
	private readonly ConcurrentQueue<MySqlStreamStoreFixture> _fixturePool = new();

	public async Task<MySqlStreamStoreFixture> Get(ILoggerFactory loggerFactory) {
		var logger = loggerFactory.CreateLogger<MySqlStreamStoreFixturePool>();

		if (!_fixturePool.TryDequeue(out var fixture)) {
			var dbUniqueName = (DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;
			var databaseName = $"sss-v3-{dbUniqueName}";
			var dockerInstance = new MySqlContainer(databaseName);
			await dockerInstance.Start();
			await dockerInstance.CreateDatabase();

			fixture = new MySqlStreamStoreFixture(
				dockerInstance,
				databaseName,
				onDispose: () => _fixturePool.Enqueue(fixture!),
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
