namespace SqlStreamStore;

using System;

internal struct StreamIdInfo // Love this name
{
	public readonly PostgresqlStreamId PostgresqlStreamId;

	public readonly PostgresqlStreamId MetadataPosgresqlStreamId;

	public StreamIdInfo(string idOriginal) {
		ArgumentException.ThrowIfNullOrWhiteSpace(idOriginal);

		PostgresqlStreamId = new PostgresqlStreamId(idOriginal);
		MetadataPosgresqlStreamId = new PostgresqlStreamId("$$" + idOriginal);
	}
}