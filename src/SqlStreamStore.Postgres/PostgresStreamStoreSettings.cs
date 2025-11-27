namespace SqlStreamStore;

using System;
using Microsoft.Extensions.Logging;
using Npgsql;
using SqlStreamStore.Infrastructure;
using SqlStreamStore.Subscriptions;

public class PostgresStreamStoreSettings {
	private string _schema = "public";
	private GetUtcNow _getUtcNow = SystemClock.GetUtcNow;

	/// <summary>
	/// Initializes a new instance of <see cref="PostgresStreamStoreSettings"/>.
	/// </summary>
	/// <param name="connectionString">The connection string.</param>
	/// <param name="loggerFactory"></param>
	public PostgresStreamStoreSettings(string connectionString, ILoggerFactory? loggerFactory = null) {
		ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

		CreateStreamStoreNotifier = store => new PollingStreamStoreNotifier(store, loggerFactory?.CreateLogger<PostgresStreamStore>());

		ConnectionString = connectionString;
		LoggerFactory = loggerFactory;
	}

	/// <summary>
	///     Gets the connection string.
	/// </summary>
	public string ConnectionString { get; }

	public ILoggerFactory? LoggerFactory { get; }

	/// <summary>
	///     Allows overriding of the stream store notifier. The default implementation
	///     creates <see cref="PollingStreamStoreNotifier"/>.
	/// </summary>
	public CreateStreamStoreNotifier CreateStreamStoreNotifier { get; set; }

	/// <summary>
	///     The schema SQL Stream Store should place database objects into. Defaults to "public".
	/// </summary>
	public string Schema {
		get => _schema;
		set {
			ArgumentException.ThrowIfNullOrWhiteSpace(value);

			_schema = value;
		}
	}

	/// <summary>
	///     Loads the auto_explain module and turns it on for all queries. Useful for index tuning.
	/// </summary>
	public bool ExplainAnalyze { get; set; }

	/// <summary>
	///     A delegate to return the current UTC now. Used in testing to
	///     control timestamps and time related operations.
	/// </summary>
	public GetUtcNow GetUtcNow {
		get => _getUtcNow;
		set => _getUtcNow = value;
	}

	/// <summary>
	///     The log name used for any of the log messages.
	/// </summary>
	public string LogName { get; } = nameof(PostgresStreamStore);

	/// <summary>
	///     Allows setting whether or not deleting expired (i.e., older than maxCount) messages happens in the same database transaction as append to stream or not.
	///     This does not effect scavenging when setting a stream's metadata - it is always run in the same transaction.
	/// </summary>
	public bool ScavengeAsynchronously { get; set; } = true;

	/// <summary>
	///     Allows overriding the way a <see cref="NpgsqlConnection"/> is created given a connection string.
	///     The default implementation simply passes the connection string into the <see cref="NpgsqlConnection"/> constructor.
	/// </summary>
	[Obsolete("Upstream changes break this", true)]
	public Func<string, NpgsqlConnection> ConnectionFactory {
		get => throw new NotSupportedException();
		set => throw new NotSupportedException();
	}

	/// <summary>
	///     Disables stream and message deletion tracking. Will increase
	///     performance, however subscribers won't know if a stream or a
	///     message has been deleted. This can be modified at runtime.
	/// </summary>
	public bool DisableDeletionTracking { get; set; }
}