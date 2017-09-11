namespace SqlStreamStore
{
    using System;
    using System.Data.SqlClient;
    using System.Threading.Tasks;
    using MySql.Data.MySqlClient;
    using SqlStreamStore.Infrastructure;
    using MySqlCommand = MySql.Data.MySqlClient.MySqlCommand;
    using MySqlConnectionStringBuilder = MySql.Data.MySqlClient.MySqlConnectionStringBuilder;

    public class MySqlStreamStoreFixture : StreamStoreAcceptanceTestFixture
    {
        public readonly string ConnectionString;
        private readonly string _schema;
        private readonly string _databaseName;
        private readonly ILocalInstance _localInstance;

        public MySqlStreamStoreFixture(string schema)
        {
            _schema = schema;
            _localInstance = new LocalInstance();

            var uniqueName = Guid.NewGuid().ToString().Replace("-", string.Empty);
            _databaseName = $"StreamStoreTests-{uniqueName}";

            ConnectionString = CreateConnectionString();
        }

        public override async Task<IStreamStore> GetStreamStore()
        {
            await CreateDatabase();

            return await GetStreamStore(_schema);
        }

        public async Task<IStreamStore> GetStreamStore(string schema)
        {
            var settings = new MySqlStreamStoreSettings(ConnectionString)
            {
                Schema = schema,
                GetUtcNow = () => GetUtcNow()
            };
            var store = new MySqlStreamStore(settings);
            await store.CreateSchema();

            return store;
        }

        public async Task<MySqlStreamStore> GetUninitializedStreamStore()
        {
            await CreateDatabase();
            
            return new MySqlStreamStore(new MySqlStreamStoreSettings(ConnectionString)
            {
                Schema = _schema,
                GetUtcNow = () => GetUtcNow()
            });
        }

        public async Task<MySqlStreamStore> GetMySqlStreamStore()
        {
            await CreateDatabase();

            var settings = new MySqlStreamStoreSettings(ConnectionString)
            {
                Schema = _schema,
                GetUtcNow = () => GetUtcNow()
            };

            var store = new MySqlStreamStore(settings);
            await store.CreateSchema();

            return store;
        }

        public override void Dispose()
        {
            using(var connection = new MySqlConnection(ConnectionString))
            {
                // Fixes: "Cannot drop database because it is currently in use"
                MySqlConnection.ClearAllPools();
            }

            using (var connection = _localInstance.CreateConnection())
            {
                connection.Open();
                using (var command = new MySqlCommand($"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", connection))
                {
                    command.ExecuteNonQuery();
                }
                using (var command = new MySqlCommand($"DROP DATABASE [{_databaseName}]", connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private async Task CreateDatabase()
        {
            using(var connection = _localInstance.CreateConnection())
            {
                await connection.OpenAsync().NotOnCapturedContext();
                var tempPath = Environment.GetEnvironmentVariable("Temp");
                var createDatabase = $"CREATE DATABASE [{_databaseName}] on (name='{_databaseName}', "
                                     + $"filename='{tempPath}\\{_databaseName}.mdf')";
                using (var command = new MySqlCommand(createDatabase, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private string CreateConnectionString()
        {
            var connectionStringBuilder = _localInstance.CreateConnectionStringBuilder();

            return connectionStringBuilder.ToString();
        }

        private interface ILocalInstance
        {
            MySql.Data.MySqlClient.MySqlConnection CreateConnection();
            MySqlConnectionStringBuilder CreateConnectionStringBuilder();
        }

        private class LocalInstance : ILocalInstance
        {
            private readonly string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=SSPI;";

            public MySql.Data.MySqlClient.MySqlConnection CreateConnection()
            {
                return new MySql.Data.MySqlClient.MySqlConnection(connectionString);
            }

            public MySqlConnectionStringBuilder CreateConnectionStringBuilder()
            {
                return new MySqlConnectionStringBuilder(connectionString);
            }
        }
    }
}