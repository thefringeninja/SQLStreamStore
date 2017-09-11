namespace SqlStreamStore
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using MySql.Data.MySqlClient;
    using SqlStreamStore.Streams;
    using SqlStreamStore.Infrastructure;
    using static Streams.Deleted;

    public partial class MySqlStreamStore
    {
        protected override Task DeleteStreamInternal(
            string streamId,
            int expectedVersion,
            CancellationToken cancellationToken)
        {
            var streamIdInfo = new StreamIdInfo(streamId);

            return expectedVersion == ExpectedVersion.Any
                ? DeleteStreamAnyVersion(streamIdInfo, cancellationToken)
                : DeleteStreamExpectedVersion(streamIdInfo, expectedVersion, cancellationToken);
        }

        protected override async Task DeleteEventInternal(
            string streamId,
            Guid eventId,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private async Task DeleteStreamExpectedVersion(
            StreamIdInfo streamIdInfo,
            int expectedVersion,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private async Task DeleteStreamAnyVersion(
            StreamIdInfo streamIdInfo,
            CancellationToken cancellationToken)
        {
            using (var connection = _createConnection())
            {
                await connection.OpenAsync(cancellationToken);

                using (var transaction = connection.BeginTransaction())
                {
                    await DeleteStreamAnyVersion(connection, transaction, streamIdInfo.SqlStreamId, cancellationToken);

                    // Delete metadata stream (if it exists)
                    await DeleteStreamAnyVersion(connection, transaction, streamIdInfo.MetadataSqlStreamId, cancellationToken);

                    transaction.Commit();
                }
            }
        }

        private async Task DeleteStreamAnyVersion(
           MySqlConnection connection,
           MySqlTransaction transaction,
           SqlStreamId sqlStreamId,
           CancellationToken cancellationToken)
        {
            bool aStreamIsDeleted;
            using (var command = new MySqlCommand(_scripts.DeleteStreamAnyVersion, connection, transaction))
            {
                command.Parameters.AddWithValue("streamId", sqlStreamId.Id);
                var i = await command
                    .ExecuteScalarAsync(cancellationToken)
                    .NotOnCapturedContext();

                aStreamIsDeleted = (int)i > 0;
            }

            if(aStreamIsDeleted)
            {
                var streamDeletedEvent = CreateStreamDeletedMessage(sqlStreamId.IdOriginal);
                await AppendToStreamExpectedVersionAny(
                    connection,
                    transaction,
                    SqlStreamId.Deleted,
                    new[] { streamDeletedEvent },
                    cancellationToken);
            }
        }
    }
}
