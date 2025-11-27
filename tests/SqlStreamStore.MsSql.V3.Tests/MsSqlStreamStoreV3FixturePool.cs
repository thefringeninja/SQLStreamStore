namespace SqlStreamStore;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SqlStreamStore.TestUtils.MsSql;
using Xunit;

public class MsSqlStreamStoreV3FixturePool : IAsyncLifetime, IDisposable {
	private readonly SqlServerContainer _dockerInstance = new(SqlServerContainer.DefaultHostPortStart + 500);

	public async Task<MsSqlStreamStoreV3Fixture> Get(
		ILoggerFactory loggerFactory,
		string schema = "dbo") {
		var databaseName = $"sss-v3-{Guid.NewGuid():N}";
		await _dockerInstance.CreateDatabase(databaseName);
		var fixture = new MsSqlStreamStoreV3Fixture(
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

	Task IAsyncLifetime.DisposeAsync() {
		return Task.CompletedTask;
	}

	public void Dispose() => _dockerInstance.Dispose();
}
