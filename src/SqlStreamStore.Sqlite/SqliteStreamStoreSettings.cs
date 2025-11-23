[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("SqlStreamStore.SqliteStreamStore.Tests")]
namespace SqlStreamStore;

using System;
using Microsoft.Extensions.Logging;
using SqlStreamStore.Infrastructure;
using SqlStreamStore.Subscriptions;

public class SqliteStreamStoreSettings {
	public ILoggerFactory LoggerFactory { get; }
	private readonly Func<bool, string> _connectionStringFactory;
	private GetUtcNow _getUtcNow;

	public SqliteStreamStoreSettings(string connectionString, ILoggerFactory loggerFactory = null) {
		ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
		CreateStreamStoreNotifier = store => new PollingStreamStoreNotifier(store, loggerFactory?.CreateLogger<SqliteStreamStore>());
		LoggerFactory = loggerFactory;

		_connectionStringFactory = (isReadOnly) => {
			if (connectionString.ToLower().Contains("read only=true")) {
				return string.Join(";", connectionString, "Read Only=True");
			}
			return connectionString;
		};
	}

	public SqliteStreamStoreSettings(Func<bool, string> connectionStringFactory) {
		ArgumentNullException.ThrowIfNull(connectionStringFactory);
		_connectionStringFactory = connectionStringFactory;
	}

	public CreateStreamStoreNotifier CreateStreamStoreNotifier { get; set; }

	/// <summary>
	///     A delegate to return the current UTC now. Used in testing to
	///     control timestamps and time related operations.
	/// </summary>
	public GetUtcNow GetUtcNow {
		get => _getUtcNow;
		set => _getUtcNow = value ?? SystemClock.GetUtcNow;
	}

	/// <summary>
	///     To help with perf, the max age of messages in a stream
	///     are cached. It is not expected that a streams max age
	///     metadata to be changed frequently. Here we hold on to the
	///     max age for the specified timespan. The default is 1 minute.
	/// </summary>
	public TimeSpan MetadataMaxAgeCacheExpire { get; set; } = TimeSpan.FromMinutes(1);

	/// <summary>
	///     To help with perf, the max age of messages in a stream
	///     are cached. It is not expected that a streams max age
	///     metadata to be changed frequently. Here we define how many
	///     items are cached. The default value is 10000.
	/// </summary>
	public int MetadataMaxAgeCacheMaxSize { get; set; } = 10000;

	/// <summary>
	///     The log name used for any of the log messages.
	/// </summary>
	public string LogName { get; } = nameof(SqliteStreamStore);

	public bool DisableDeletionTracking { get; set; }

	internal string GetConnectionString(bool isReadOnly) => _connectionStringFactory.Invoke(isReadOnly);
}