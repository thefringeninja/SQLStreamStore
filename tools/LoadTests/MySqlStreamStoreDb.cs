namespace LoadTests;

using System;
using System.Threading.Tasks;
using SqlStreamStore;
using SqlStreamStore.TestUtils.MySql;

public class MySqlStreamStoreDb : IDisposable {
	private string ConnectionString => _databaseManager.ConnectionString;
	private readonly MySqlContainer _databaseManager;

	public MySqlStreamStoreDb() {
		_databaseManager = new MySqlContainer($"test_{Guid.NewGuid():n}");
	}

	public async Task<MySqlStreamStore> GetMySqlStreamStore() {
		await CreateDatabase();

		var settings = new MySqlStreamStoreSettings(ConnectionString);

		var mySqlStreamStore = new MySqlStreamStore(settings);
		await mySqlStreamStore.CreateSchemaIfNotExists();
		return mySqlStreamStore;
	}

	public Task CreateDatabase() => _databaseManager.CreateDatabase();

	public void Dispose() {

	}
}