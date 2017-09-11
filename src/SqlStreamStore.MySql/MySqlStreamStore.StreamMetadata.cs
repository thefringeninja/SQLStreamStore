namespace SqlStreamStore
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using SqlStreamStore.Streams;
    using SqlStreamStore.Infrastructure;
    using StreamStoreStore.Json;

    public partial class MySqlStreamStore
    {
        protected override async Task<StreamMetadataResult> GetStreamMetadataInternal(
            string streamId,
            CancellationToken cancellationToken)
        {
            var streamIdInfo = new StreamIdInfo(streamId);

            ReadStreamPage page;
            using (var connection = _createConnection())
            {
                await connection.OpenAsync(cancellationToken).NotOnCapturedContext();
                page = await ReadStreamInternal(
                    streamIdInfo.MetadataSqlStreamId,
                    StreamVersion.End,
                    1,
                    ReadDirection.Backward,
                    true,
                    null,
                    connection,
                    cancellationToken);
            }

            if(page.Status == PageReadStatus.StreamNotFound)
            {
                return new StreamMetadataResult(streamId, -1);
            }

            var metadataMessage = await page.Messages[0].GetJsonDataAs<MetadataMessage>(cancellationToken);

            return new StreamMetadataResult(
                   streamId,
                   page.LastStreamVersion,
                   metadataMessage.MaxAge,
                   metadataMessage.MaxCount,
                   metadataMessage.MetaJson);
        }

        protected override async Task<SetStreamMetadataResult> SetStreamMetadataInternal(
            string streamId,
            int expectedStreamMetadataVersion,
            int? maxAge,
            int? maxCount,
            string metadataJson,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
