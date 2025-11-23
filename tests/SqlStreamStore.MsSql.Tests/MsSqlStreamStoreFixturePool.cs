namespace SqlStreamStore;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SqlStreamStore.TestUtils.MsSql;
using Xunit;

public class MsSqlStreamStoreFixturePool : IAsyncLifetime, IDisposable {
	private readonly SqlServerContainer _dockerInstance = new();

	public async Task<MsSqlStreamStoreFixture> Get(
		ILoggerFactory loggerFactory,
		string schema = "dbo") {
		var databaseName = $"sss-v2-{Guid.NewGuid():N}";
		await _dockerInstance.CreateDatabase(databaseName);
		var fixture = new MsSqlStreamStoreFixture(
			schema,
			_dockerInstance,
			databaseName,
			onDispose: () => {},
			loggerFactory);

		await fixture.Prepare();

		return fixture;
	}

	public async Task InitializeAsync() {
		await _dockerInstance.Start();
	}

	Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

	public void Dispose() => _dockerInstance.Dispose();
}
