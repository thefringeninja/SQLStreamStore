namespace SqlStreamStore.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using SqlStreamStore.Logging;
    using SqlStreamStore.Streams;
    using SqlStreamStore.Subscriptions;
    using SqlStreamStore;
    using SqlStreamStore.Imports.Ensure.That;

    /// <summary>
    ///     Represents a base implementation of a readonly stream store.
    /// </summary>
    public abstract class ReadonlyStreamStoreBase : IReadonlyStreamStore
    {
        private const int DefaultReloadInterval = 3000;
        protected readonly GetUtcNow GetUtcNow;
        protected readonly ILog Logger;
        private bool _isDisposed;
        private readonly MetadataMaxAgeCache _metadataMaxAgeCache;
        private readonly bool _disableMetadataCache;

        protected ReadonlyStreamStoreBase(
            TimeSpan metadataMaxAgeCacheExpiry,
            int metadataMaxAgeCacheMaxSize,
            GetUtcNow getUtcNow,
            string logName)
        {
            GetUtcNow = getUtcNow ?? SystemClock.GetUtcNow;
            Logger = LogProvider.GetLogger(logName);

            _metadataMaxAgeCache = new MetadataMaxAgeCache(this,
                metadataMaxAgeCacheExpiry,
                metadataMaxAgeCacheMaxSize,
                GetUtcNow);
        }

        protected ReadonlyStreamStoreBase(GetUtcNow getUtcNow, string logName)
        {
            GetUtcNow = getUtcNow ?? SystemClock.GetUtcNow;
            Logger = LogProvider.GetLogger(logName);
            _disableMetadataCache = true;
        }

        public async Task<IAsyncEnumerable<StreamMessage>> ReadAllForwards(
            long fromPositionInclusive,
            int pageSize,
            bool prefetchJsonData,
            CancellationToken cancellationToken = default)
        {
            Ensure.That(fromPositionInclusive, nameof(fromPositionInclusive)).IsGte(0);
            Ensure.That(pageSize, nameof(pageSize)).IsGte(1);

            GuardAgainstDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            Logger.DebugFormat("ReadAllForwards from position {fromPositionInclusive} with max count " +
                               "{maxCount}.",
                fromPositionInclusive,
                pageSize);

            Task<ReadAllPage> ReadNext(long nextPosition, CancellationToken ct) =>
                ReadAllForwardsInternal(nextPosition, pageSize, prefetchJsonData, ReadNext, ct);

            var page = await ReadAllForwardsInternal(
                    fromPositionInclusive,
                    pageSize,
                    prefetchJsonData,
                    ReadNext,
                    cancellationToken)
                .NotOnCapturedContext();

            return ReadAllForwards(page, cancellationToken);
        }

        private async IAsyncEnumerable<StreamMessage> ReadAllForwards(
            ReadAllPage page,
            CancellationToken cancellationToken)
        {
            while(page.Messages.Length != 0)
            {
                var lastMessage = default(StreamMessage);

                await foreach(var message in FilterExpired(page, cancellationToken))
                {
                    if(message.Position != lastMessage.Position + 1)
                    {
                        // reload after delay?
                    }

                    yield return message;
                    lastMessage = message;
                }

                if(page.IsEnd)
                {
                    break;
                }

                page = await page.ReadNext(cancellationToken).NotOnCapturedContext();
            }
        }

        public async Task<IAsyncEnumerable<StreamMessage>> ReadAllBackwards(
            long fromPositionInclusive,
            int maxCount,
            bool prefetchJsonData,
            CancellationToken cancellationToken = default)
        {
            Ensure.That(fromPositionInclusive, nameof(fromPositionInclusive)).IsGte(-1);
            Ensure.That(maxCount, nameof(maxCount)).IsGte(1);

            GuardAgainstDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            Logger.Debug(
                "ReadAllBackwards from position {fromPositionInclusive} with max count {maxCount}.",
                fromPositionInclusive,
                maxCount);

            Task<ReadAllPage> ReadNext(long nextPosition, CancellationToken ct) => ReadAllBackwardsInternal(
                nextPosition,
                maxCount,
                prefetchJsonData,
                ReadNext,
                ct);

            var page = await ReadAllBackwardsInternal(
                fromPositionInclusive,
                maxCount,
                prefetchJsonData,
                ReadNext,
                cancellationToken);

            return ReadAllBackwards(page, cancellationToken);
        }

        private async IAsyncEnumerable<StreamMessage> ReadAllBackwards(
            ReadAllPage page,
            CancellationToken cancellationToken)
        {
            while(page.Messages.Length > 0)
            {
                await foreach(var message in FilterExpired(page, cancellationToken))
                {
                    yield return message;
                }

                if(page.IsEnd)
                {
                    break;
                }

                page = await page.ReadNext(cancellationToken).NotOnCapturedContext();
            }
        }

        public async Task<ReadStreamResult> ReadStreamForwards(
            StreamId streamId,
            int fromVersionInclusive,
            int maxCount,
            bool prefetchJsonData = true,
            CancellationToken cancellationToken = default)
        {
            Ensure.That(fromVersionInclusive, nameof(fromVersionInclusive)).IsGte(0);
            Ensure.That(maxCount, nameof(maxCount)).IsGte(1);

            GuardAgainstDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            Logger.Debug(
                "ReadStreamForwards {streamId} from version {fromVersionInclusive} with max count {maxCount}.",
                streamId,
                fromVersionInclusive,
                maxCount);

            Task<ReadStreamPage> ReadNext(int nextVersion, CancellationToken ct) =>
                ReadStreamForwardsInternal(streamId, nextVersion, maxCount, prefetchJsonData, ReadNext, ct);

            var page = await ReadStreamForwardsInternal(
                streamId,
                fromVersionInclusive,
                maxCount,
                prefetchJsonData,
                ReadNext,
                cancellationToken);

            return page.Status == PageReadStatus.StreamNotFound
                ? ReadStreamResult.NotFound
                : new ReadStreamResult(ReadStream(page, cancellationToken), page.LastStreamVersion);
        }

        public async Task<ReadStreamResult> ReadStreamBackwards(
            StreamId streamId,
            int fromVersionInclusive,
            int maxCount,
            bool prefetchJsonData = true,
            CancellationToken cancellationToken = default)
        {
            Ensure.That(fromVersionInclusive, nameof(fromVersionInclusive)).IsGte(-1);
            Ensure.That(maxCount, nameof(maxCount)).IsGte(1);

            GuardAgainstDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            Logger.Debug(
                "ReadStreamBackwards {streamId} from version {fromVersionInclusive} with max count {maxCount}.",
                streamId,
                fromVersionInclusive,
                maxCount);

            Task<ReadStreamPage> ReadNext(int nextVersion, CancellationToken ct)
                => ReadStreamBackwardsInternal(streamId, nextVersion, maxCount, prefetchJsonData, ReadNext, ct);

            var page = await ReadStreamBackwardsInternal(
                streamId,
                fromVersionInclusive,
                maxCount,
                prefetchJsonData,
                ReadNext,
                cancellationToken);

            return page.Status == PageReadStatus.StreamNotFound
                ? ReadStreamResult.NotFound
                : new ReadStreamResult(ReadStream(page, cancellationToken), page.LastStreamVersion);
        }

        private async IAsyncEnumerable<StreamMessage> ReadStream(
            ReadStreamPage page,
            CancellationToken cancellationToken)
        {
            while(page.Messages.Length > 0)
            {
                await foreach(var message in FilterExpired(page, cancellationToken))
                {
                    yield return message;
                }

                if(page.IsEnd)
                {
                    break;
                }

                page = await page.ReadNext(cancellationToken).NotOnCapturedContext();
            }
        }

        public IStreamSubscription SubscribeToStream(
            StreamId streamId,
            int? continueAfterVersion,
            StreamMessageReceived streamMessageReceived,
            SubscriptionDropped subscriptionDropped = null,
            HasCaughtUp hasCaughtUp = null,
            bool prefetchJsonData = true,
            string name = null)
        {
            Ensure.That(streamId, nameof(streamId)).IsNotNull();
            Ensure.That(streamMessageReceived, nameof(streamMessageReceived)).IsNotNull();

            GuardAgainstDisposed();

            return SubscribeToStreamInternal(
                streamId,
                continueAfterVersion,
                streamMessageReceived,
                subscriptionDropped,
                hasCaughtUp,
                prefetchJsonData,
                name);
        }

        public IAllStreamSubscription SubscribeToAll(
            long? continueAfterPosition,
            AllStreamMessageReceived streamMessageReceived,
            AllSubscriptionDropped subscriptionDropped = null,
            HasCaughtUp hasCaughtUp = null,
            bool prefetchJsonData = true,
            string name = null)
        {
            Ensure.That(streamMessageReceived, nameof(streamMessageReceived)).IsNotNull();

            GuardAgainstDisposed();

            return SubscribeToAllInternal(
                continueAfterPosition,
                streamMessageReceived,
                subscriptionDropped,
                hasCaughtUp,
                prefetchJsonData,
                name);
        }

        public Task<StreamMetadataResult> GetStreamMetadata(
            string streamId,
            CancellationToken cancellationToken = default)
        {
            if(streamId == null) throw new ArgumentNullException(nameof(streamId));
            if(streamId.StartsWith("$") && streamId != Deleted.DeletedStreamId)
            {
                throw new ArgumentException("Must not start with '$'", nameof(streamId));
            }

            Logger.Debug("GetStreamMetadata {streamId}.", streamId);

            return GetStreamMetadataInternal(streamId, cancellationToken);
        }

        public Task<long> ReadHeadPosition(CancellationToken cancellationToken = default)
        {
            GuardAgainstDisposed();

            return ReadHeadPositionInternal(cancellationToken);
        }

        public Task<ListStreamsPage> ListStreams(
            int maxCount = 100,
            string continuationToken = default,
            CancellationToken cancellationToken = default)
        {
            Ensure.That(maxCount).IsGt(0);

            GuardAgainstDisposed();

            return ListStreams(Pattern.Anything(), maxCount, continuationToken, cancellationToken);
        }

        public Task<ListStreamsPage> ListStreams(
            Pattern pattern,
            int maxCount = 100,
            string continuationToken = default,
            CancellationToken cancellationToken = default)
        {
            Ensure.That(maxCount).IsGt(0);
            Ensure.That(pattern).IsNotNull();

            Task<ListStreamsPage> ListNext(string @continue, CancellationToken ct)
                => ListStreams(pattern, maxCount, @continue, ct);

            return ListStreamsInternal(pattern, maxCount, continuationToken, ListNext, cancellationToken);
        }

        public void Dispose()
        {
            OnDispose?.Invoke();
            Dispose(true);
            GC.SuppressFinalize(this);
            _isDisposed = true;
        }

        public event Action OnDispose;

        protected abstract Task<ReadAllPage> ReadAllForwardsInternal(
            long fromPositionExclusive,
            int maxCount,
            bool prefetch,
            ReadNextAllPage readNext,
            CancellationToken cancellationToken);

        protected abstract Task<ReadAllPage> ReadAllBackwardsInternal(
            long fromPositionExclusive,
            int maxCount,
            bool prefetch,
            ReadNextAllPage readNext,
            CancellationToken cancellationToken);

        protected abstract Task<ReadStreamPage> ReadStreamForwardsInternal(
            string streamId,
            int start,
            int count,
            bool prefetch,
            ReadNextStreamPage readNext,
            CancellationToken cancellationToken);

        protected abstract Task<ReadStreamPage> ReadStreamBackwardsInternal(
            string streamId,
            int fromVersionInclusive,
            int count,
            bool prefetch,
            ReadNextStreamPage readNext,
            CancellationToken cancellationToken);

        protected abstract Task<long> ReadHeadPositionInternal(CancellationToken cancellationToken);

        protected abstract IStreamSubscription SubscribeToStreamInternal(
            string streamId,
            int? startVersion,
            StreamMessageReceived streamMessageReceived,
            SubscriptionDropped subscriptionDropped,
            HasCaughtUp hasCaughtUp,
            bool prefetchJsonData,
            string name);

        protected abstract IAllStreamSubscription SubscribeToAllInternal(
            long? fromPosition,
            AllStreamMessageReceived streamMessageReceived,
            AllSubscriptionDropped subscriptionDropped,
            HasCaughtUp hasCaughtUp,
            bool prefetchJsonData,
            string name);

        protected abstract Task<StreamMetadataResult> GetStreamMetadataInternal(
            string streamId,
            CancellationToken cancellationToken);

        protected abstract Task<ListStreamsPage> ListStreamsInternal(
            Pattern pattern,
            int maxCount,
            string continuationToken,
            ListNextStreamsPage listNextStreamsPage,
            CancellationToken cancellationToken);

        protected virtual void Dispose(bool disposing)
        { }

        protected void GuardAgainstDisposed()
        {
            if(_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        protected abstract void PurgeExpiredMessage(StreamMessage streamMessage);

        private async Task<ReadStreamPage> FilterExpired(
            ReadStreamPage page,
            ReadNextStreamPage readNext,
            CancellationToken cancellationToken)
        {
            if(page.StreamId.StartsWith("$"))
            {
                return page;
            }

            int? maxAge = _metadataMaxAgeCache == null
                ? null
                : await _metadataMaxAgeCache.GetMaxAge(page.StreamId, cancellationToken);
            if(!maxAge.HasValue)
            {
                return page;
            }

            var currentUtc = GetUtcNow();
            var valid = new List<StreamMessage>();
            foreach(var message in page.Messages)
            {
                if(message.CreatedUtc.AddSeconds(maxAge.Value) > currentUtc)
                {
                    valid.Add(message);
                }
                else
                {
                    PurgeExpiredMessage(message);
                }
            }

            return new ReadStreamPage(
                page.StreamId,
                page.Status,
                page.FromStreamVersion,
                page.NextStreamVersion,
                page.LastStreamVersion,
                page.LastStreamPosition,
                page.ReadDirection,
                page.IsEnd,
                readNext,
                valid.ToArray());
        }

        private async IAsyncEnumerable<StreamMessage> FilterExpired(
            ReadAllPage readAllPage,
            CancellationToken cancellationToken)
        {
            if(_disableMetadataCache)
            {
                foreach(var message in readAllPage.Messages)
                {
                    yield return message;
                }
            }

            var currentUtc = GetUtcNow();
            foreach(var streamMessage in readAllPage.Messages)
            {
                if(streamMessage.StreamId.StartsWith("$"))
                {
                    yield return streamMessage;
                    continue;
                }

                int? maxAge = _metadataMaxAgeCache == null
                    ? null
                    : await _metadataMaxAgeCache.GetMaxAge(streamMessage.StreamId, cancellationToken);
                if(!maxAge.HasValue)
                {
                    yield return streamMessage;
                    continue;
                }

                if(streamMessage.CreatedUtc.AddSeconds(maxAge.Value) > currentUtc)
                {
                    yield return streamMessage;
                }
                else
                {
                    PurgeExpiredMessage(streamMessage);
                }
            }
        }

        private async IAsyncEnumerable<StreamMessage> FilterExpired(
            ReadStreamPage page,
            CancellationToken cancellationToken)
        {
            if(_disableMetadataCache)
            {
                foreach(var message in page.Messages)
                {
                    yield return message;
                }
            }

            var currentUtc = GetUtcNow();
            foreach(var streamMessage in page.Messages)
            {
                if(streamMessage.StreamId.StartsWith("$"))
                {
                    yield return streamMessage;
                    continue;
                }

                int? maxAge = _metadataMaxAgeCache == null
                    ? null
                    : await _metadataMaxAgeCache.GetMaxAge(streamMessage.StreamId, cancellationToken);
                if(!maxAge.HasValue)
                {
                    yield return streamMessage;
                    continue;
                }

                if(streamMessage.CreatedUtc.AddSeconds(maxAge.Value) > currentUtc)
                {
                    yield return streamMessage;
                }
                else
                {
                    PurgeExpiredMessage(streamMessage);
                }
            }
        }

        protected List<StreamMessage> FilterExpired(List<(StreamMessage StreamMessage, int? MaxAge)> messages)
        {
            var valid = new List<StreamMessage>();
            var currentUtc = GetUtcNow();
            foreach(var item in messages)
            {
                if(item.StreamMessage.StreamId.StartsWith("$"))
                {
                    valid.Add(item.StreamMessage);
                    continue;
                }

                if(!item.MaxAge.HasValue || item.MaxAge <= 0)
                {
                    valid.Add(item.StreamMessage);
                    continue;
                }

                if(item.StreamMessage.CreatedUtc.AddSeconds(item.MaxAge.Value) > currentUtc)
                {
                    valid.Add(item.StreamMessage);
                }
                else
                {
                    PurgeExpiredMessage(item.StreamMessage);
                }
            }

            return valid;
        }

        ~ReadonlyStreamStoreBase()
        {
            Dispose(false);
        }
    }
}