namespace SqlStreamStore;

using System;

internal struct StreamIdInfo // Love this name
{
	public static readonly StreamIdInfo Deleted = new(Streams.Deleted.DeletedStreamId);

	public readonly MySqlStreamId MySqlStreamId;

	public readonly MySqlStreamId MetadataMySqlStreamId;

	public StreamIdInfo(string idOriginal) {
		ArgumentException.ThrowIfNullOrWhiteSpace(idOriginal);

		MySqlStreamId = new MySqlStreamId(idOriginal);
		MetadataMySqlStreamId = new MySqlStreamId("$$" + idOriginal);
	}
}
