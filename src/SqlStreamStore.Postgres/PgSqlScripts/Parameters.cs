namespace SqlStreamStore.PgSqlScripts;

using System;
using Npgsql;
using NpgsqlTypes;
using SqlStreamStore.Infrastructure;
using SqlStreamStore.Streams;

internal static class Parameters {
	private const int StreamIdSize = 42;
	private const int OriginalStreamIdSize = 1000;

	public static NpgsqlParameter DeletedStreamId =>
		new NpgsqlParameter<string> {
			ParameterName = nameof(DeletedStreamId),
			NpgsqlDbType = NpgsqlDbType.Char,
			Size = StreamIdSize,
			TypedValue = PostgresqlStreamId.Deleted.Id
		};

	public static NpgsqlParameter DeletedStreamIdOriginal =>
		new NpgsqlParameter<string> {
			ParameterName = nameof(DeletedStreamIdOriginal),
			NpgsqlDbType = NpgsqlDbType.Varchar,
			Size = OriginalStreamIdSize,
			TypedValue = PostgresqlStreamId.Deleted.IdOriginal
		};

	public static NpgsqlParameter StreamId(PostgresqlStreamId value) =>
		new NpgsqlParameter<string> {
			ParameterName = nameof(StreamId),
			NpgsqlDbType = NpgsqlDbType.Char,
			Size = StreamIdSize,
			TypedValue = value.Id
		};

	public static NpgsqlParameter StreamIdOriginal(PostgresqlStreamId value) =>
		new NpgsqlParameter<string> {
			ParameterName = nameof(StreamIdOriginal),
			NpgsqlDbType = NpgsqlDbType.Varchar,
			Size = OriginalStreamIdSize,
			TypedValue = value.IdOriginal
		};

	public static NpgsqlParameter MetadataStreamId(PostgresqlStreamId value) =>
		new NpgsqlParameter<string> {
			ParameterName = nameof(MetadataStreamId),
			NpgsqlDbType = NpgsqlDbType.Char,
			Size = StreamIdSize,
			TypedValue = value.Id
		};

	public static NpgsqlParameter MetadataStreamIdOriginal(PostgresqlStreamId value) =>
		new NpgsqlParameter<string> {
			ParameterName = nameof(MetadataStreamIdOriginal),
			NpgsqlDbType = NpgsqlDbType.Char,
			Size = StreamIdSize,
			TypedValue = value.IdOriginal
		};

	public static NpgsqlParameter DeletedMessages(PostgresqlStreamId streamId, params Guid[] messageIds) {
		return new NpgsqlParameter<PostgresNewStreamMessage[]> {
			ParameterName = nameof(DeletedMessages),
			TypedValue = Array.ConvertAll(
				messageIds,
				messageId => PostgresNewStreamMessage.FromNewStreamMessage(
					Deleted.CreateMessageDeletedMessage(streamId.IdOriginal, messageId))
			)
		};
	}

	public static NpgsqlParameter DeletedStreamMessage(PostgresqlStreamId streamId) =>
		new NpgsqlParameter<PostgresNewStreamMessage> {
			ParameterName = nameof(DeletedStreamMessage),
			TypedValue = PostgresNewStreamMessage.FromNewStreamMessage(
				Deleted.CreateStreamDeletedMessage(streamId.IdOriginal))
		};

	public static NpgsqlParameter ExpectedVersion(int value) =>
		new NpgsqlParameter<int> {
			ParameterName = nameof(ExpectedVersion),
			NpgsqlDbType = NpgsqlDbType.Integer,
			TypedValue = value
		};

	public static NpgsqlParameter CreatedUtc(DateTime? value) =>
		value.HasValue
			? new NpgsqlParameter<DateTime> {
				ParameterName = nameof(CreatedUtc),
				TypedValue = DateTime.SpecifyKind(value.Value, DateTimeKind.Unspecified),
				NpgsqlDbType = NpgsqlDbType.Timestamp
			}
			: new NpgsqlParameter<DBNull> {
				ParameterName = nameof(CreatedUtc),
				TypedValue = DBNull.Value,
				NpgsqlDbType = NpgsqlDbType.Timestamp
			};

	public static NpgsqlParameter NewStreamMessages(NewStreamMessage[] value) =>
		new NpgsqlParameter<PostgresNewStreamMessage[]> {
			ParameterName = nameof(NewStreamMessages),
			TypedValue = Array.ConvertAll(value, PostgresNewStreamMessage.FromNewStreamMessage)
		};

	public static NpgsqlParameter MetadataStreamMessage(
		PostgresqlStreamId streamId,
		int expectedVersion,
		MetadataMessage value) {
		var jsonData = SimpleJson.SerializeObject(value);
		return new NpgsqlParameter<PostgresNewStreamMessage> {
			ParameterName = nameof(MetadataStreamMessage),
			TypedValue = PostgresNewStreamMessage.FromNewStreamMessage(
				new NewStreamMessage(
					MetadataMessageIdGenerator.Create(streamId.IdOriginal, expectedVersion, jsonData),
					"$stream-metadata",
					jsonData))
		};
	}

	public static NpgsqlParameter Count(int value) =>
		new NpgsqlParameter<int> {
			ParameterName = nameof(Count),
			NpgsqlDbType = NpgsqlDbType.Integer,
			TypedValue = value
		};

	public static NpgsqlParameter Version(int value) =>
		new NpgsqlParameter<int> {
			ParameterName = nameof(Version),
			NpgsqlDbType = NpgsqlDbType.Integer,
			TypedValue = value
		};

	public static NpgsqlParameter ReadDirection(ReadDirection direction) =>
		new NpgsqlParameter<bool> {
			ParameterName = nameof(ReadDirection),
			NpgsqlDbType = NpgsqlDbType.Boolean,
			TypedValue = direction == Streams.ReadDirection.Forward
		};

	public static NpgsqlParameter Prefetch(bool value) =>
		new NpgsqlParameter<bool> {
			ParameterName = nameof(Prefetch),
			NpgsqlDbType = NpgsqlDbType.Boolean,
			TypedValue = value
		};

	public static NpgsqlParameter MessageIds(Guid[] value) =>
		new NpgsqlParameter<Guid[]> {
			ParameterName = nameof(MessageIds),
			Value = value
		};

	public static NpgsqlParameter Position(long value) =>
		new NpgsqlParameter<long> {
			ParameterName = nameof(Position),
			NpgsqlDbType = NpgsqlDbType.Bigint,
			TypedValue = value
		};

	public static NpgsqlParameter OptionalMaxAge(int? value) =>
		new() {
			ParameterName = nameof(OptionalMaxAge),
			NpgsqlDbType = NpgsqlDbType.Integer,
			NpgsqlValue = value.HasValue ? (object)value.Value : DBNull.Value
		};

	public static NpgsqlParameter OptionalMaxCount(int? value) =>
		new() {
			ParameterName = nameof(OptionalMaxCount),
			NpgsqlDbType = NpgsqlDbType.Integer,
			NpgsqlValue = value.HasValue ? (object)value.Value : DBNull.Value
		};

	public static NpgsqlParameter MaxCount(int value) =>
		new NpgsqlParameter<int> {
			ParameterName = nameof(MaxCount),
			NpgsqlDbType = NpgsqlDbType.Integer,
			TypedValue = value
		};

	public static NpgsqlParameter OptionalStartingAt(int? value) =>
		value.HasValue
			? new NpgsqlParameter<int> {
				ParameterName = nameof(OptionalStartingAt),
				NpgsqlDbType = NpgsqlDbType.Integer,
				TypedValue = value.Value
			}
			: new NpgsqlParameter<DBNull> {
				ParameterName = nameof(OptionalStartingAt),
				TypedValue = DBNull.Value
			};

	public static NpgsqlParameter OptionalAfterIdInternal(int? value) =>
		value.HasValue
			? (NpgsqlParameter)new NpgsqlParameter<int> {
				ParameterName = nameof(OptionalAfterIdInternal),
				NpgsqlDbType = NpgsqlDbType.Integer,
				TypedValue = value.Value
			}
			: new NpgsqlParameter<DBNull> {
				ParameterName = nameof(OptionalAfterIdInternal),
				TypedValue = DBNull.Value
			};

	public static NpgsqlParameter Pattern(string value) =>
		new NpgsqlParameter<string> {
			ParameterName = nameof(Pattern),
			TypedValue = value,
			NpgsqlDbType = NpgsqlDbType.Varchar
		};

	public static NpgsqlParameter DeletionTrackingDisabled(bool deletionTrackingDisabled) =>
		new NpgsqlParameter<bool> {
			ParameterName = nameof(DeletionTrackingDisabled),
			TypedValue = deletionTrackingDisabled,
			NpgsqlDbType = NpgsqlDbType.Boolean
		};

	public static NpgsqlParameter Empty(NpgsqlParameter parameter) =>
		new() {
			ParameterName = parameter.ParameterName,
			IsNullable = true,
			Value = DBNull.Value
		};
}
