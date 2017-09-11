namespace SqlStreamStore
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using MySql.Data.MySqlClient;
    using SqlStreamStore.Imports.Ensure.That;
    using SqlStreamStore.Streams;

    public partial class MySqlStreamStore
    {
        protected override async Task<AppendResult> AppendToStreamInternal(
            string streamId,
            int expectedVersion,
            NewStreamMessage[] messages,
            CancellationToken cancellationToken)
        {
            Ensure.That(streamId, "streamId").IsNotNullOrWhiteSpace();
            Ensure.That(expectedVersion, "expectedVersion").IsGte(-2);
            Ensure.That(messages, "Messages").IsNotNull();
            GuardAgainstDisposed();

            throw new NotImplementedException();
        }

        private Task AppendToStreamExpectedVersionAny(MySqlConnection connection, MySqlTransaction transaction, SqlStreamId deleted, NewStreamMessage[] newStreamMessages, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}