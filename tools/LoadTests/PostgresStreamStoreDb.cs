namespace LoadTests;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SqlStreamStore;
using SqlStreamStore.TestUtils.Postgres;
using Xunit.Abstractions;

public class PostgresStreamStoreDb : IDisposable {
	public string ConnectionString => _databaseManager.ConnectionString;
	private readonly string _schema;
	private readonly ILoggerFactory _loggerFactory;
	private readonly PostgresDatabaseManager _databaseManager;

	public PostgresStreamStoreDb(string schema, ILoggerFactory loggerFactory) {
		_schema = schema;
		_loggerFactory = loggerFactory;

		_databaseManager = new PostgresContainer($"test_{Guid.NewGuid():n}");
	}

	public async Task<PostgresStreamStore> GetPostgresStreamStore(bool scavengeAsynchronously = false) {
		var store = await GetUninitializedPostgresStreamStore(scavengeAsynchronously);

		await store.CreateSchemaIfNotExists();

		return store;
	}

	public async Task<PostgresStreamStore> GetUninitializedPostgresStreamStore(bool scavengeAsynchronously = false) {
		await CreateDatabase();

		var settings = new PostgresStreamStoreSettings(ConnectionString, _loggerFactory) {
			Schema = _schema,
			ScavengeAsynchronously = scavengeAsynchronously
		};

		return new PostgresStreamStore(settings);
	}

	public void Dispose() {
		_databaseManager?.Dispose();
	}

	public Task CreateDatabase() => _databaseManager.CreateDatabase();

	private class ConsoleTestoutputHelper : ITestOutputHelper {
		public void WriteLine(string message) => Console.Write(message);

		public void WriteLine(string format, params object[] args) => Console.WriteLine(format, args);
	}
}