namespace SqlStreamStore.Streams
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    public struct ReadStreamResult : IAsyncEnumerable<StreamMessage>
    {
        private readonly IAsyncEnumerable<StreamMessage> _inner;
        public int LastStreamVersion { get; }
        public PageReadStatus Status { get; }

        public static readonly ReadStreamResult NotFound = new ReadStreamResult(
            AsyncEnumerable.Empty<StreamMessage>(),
            StreamVersion.End,
            PageReadStatus.StreamNotFound);

        public ReadStreamResult(
            IAsyncEnumerable<StreamMessage> inner,
            int lastStreamVersion,
            PageReadStatus status = PageReadStatus.Success)
        {
            if(inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }

            if(lastStreamVersion < StreamVersion.End)
            {
                throw new ArgumentOutOfRangeException(nameof(lastStreamVersion));
            }
            
            _inner = inner;
            LastStreamVersion = lastStreamVersion;
            Status = status;
        }

        public IAsyncEnumerator<StreamMessage> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => _inner.TakeWhile(ConsistentWithLastStreamVersion).GetAsyncEnumerator(cancellationToken);

        private bool ConsistentWithLastStreamVersion(StreamMessage message)
            => message.StreamVersion <= LastStreamVersion;
    }
}