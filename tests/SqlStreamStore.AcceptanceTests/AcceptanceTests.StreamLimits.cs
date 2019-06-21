namespace SqlStreamStore
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Shouldly;
    using SqlStreamStore.Streams;
    using Xunit;

    public partial class AcceptanceTests
    {
        [Theory, Trait("Category", "StreamMetadata"),
         InlineData(ExpectedVersion.NoStream), InlineData(ExpectedVersion.Any)]
        public async Task When_stream_has_max_count_and_append_exceeds_then_should_maintain_max_count(
            int expectedVersion)
        {
            const string streamId = "stream-1";
            const int maxCount = 2;
            await store.SetStreamMetadata(streamId, maxCount: maxCount, metadataJson: "meta");
            await store.AppendToStream(streamId, expectedVersion, CreateNewStreamMessages(1, 2, 3, 4));

            var result = await store.ReadStreamForwards(streamId, StreamVersion.Start, 4);
            var count = await result.CountAsync();

            count.ShouldBe(maxCount);
        }

        [Theory, Trait("Category", "StreamMetadata"),
         InlineData(ExpectedVersion.EmptyStream)]
        public async Task When_empty_stream_has_max_count_and_append_exceeds_then_should_maintain_max_count(
            int expectedVersion)
        {
            const string streamId = "stream-1";
            const int maxCount = 2;
            await store.SetStreamMetadata(streamId, maxCount: maxCount, metadataJson: "meta");
            await store.AppendToStream(streamId, ExpectedVersion.NoStream, new NewStreamMessage[0]);
            await store.AppendToStream(streamId, expectedVersion, CreateNewStreamMessages(1, 2, 3, 4));

            var result = await store.ReadStreamForwards(streamId, StreamVersion.Start, 4);
            var count = await result.CountAsync();

            count.ShouldBe(maxCount);
        }

        [Theory, InlineData(1, 1), InlineData(2, 2), InlineData(6, 4), Trait("Category", "StreamMetadata")]
        public async Task When_stream_max_count_is_set_then_stream_should_have_max_count(
            int maxCount,
            int expectedLength)
        {
            const string streamId = "stream-1";

            await store.AppendToStream(streamId, ExpectedVersion.NoStream, CreateNewStreamMessages(1, 2, 3, 4));
            await store.SetStreamMetadata(streamId, maxCount: maxCount, metadataJson: "meta");

            var result = await store.ReadStreamForwards(streamId, StreamVersion.Start, 4);
            var count = await result.CountAsync();

            count.ShouldBe(expectedLength);
        }

        [Fact, Trait("Category", "StreamMetadata")]
        public async Task When_stream_has_expired_messages_and_read_forwards_then_should_not_get_expired_messages()
        {
            var currentUtc = new DateTime(2016, 1, 1, 0, 0, 0);
            fixture.GetUtcNow = () => currentUtc;
            string streamId = "stream-1";
            await store
                .AppendToStream(streamId, ExpectedVersion.NoStream, CreateNewStreamMessages(1, 2, 3, 4));
            currentUtc += TimeSpan.FromSeconds(60);
            await store.AppendToStream(streamId, ExpectedVersion.Any, CreateNewStreamMessages(5, 6, 7, 8));
            await store.SetStreamMetadata(streamId, maxAge: 30, metadataJson: "meta");

            var result = await store.ReadStreamForwards(streamId, StreamVersion.Start, 8);

            var count = await result.CountAsync();

            count.ShouldBe(4);
        }

        [Fact, Trait("Category", "StreamMetadata")]
        public async Task When_stream_has_expired_messages_and_read_backward_then_should_not_get_expired_messages()
        {
            var currentUtc = new DateTime(2016, 1, 1, 0, 0, 0);
            fixture.GetUtcNow = () => currentUtc;
            const string streamId = "stream-1";
            await store
                .AppendToStream(streamId, ExpectedVersion.NoStream, CreateNewStreamMessages(1, 2, 3, 4));
            currentUtc += TimeSpan.FromSeconds(60);
            await store
                .AppendToStream(streamId, ExpectedVersion.Any, CreateNewStreamMessages(5, 6, 7, 8));
            await store
                .SetStreamMetadata(streamId, maxAge: 30, metadataJson: "meta");

            var result = await store.ReadStreamBackwards(streamId, StreamVersion.End, 8);
            var count = await result.CountAsync();

            count.ShouldBe(4);
        }

        [Fact, Trait("Category", "StreamMetadata")]
        public async Task
            When_streams_have_expired_messages_and_read_all_forwards_then_should_not_get_expired_messages()
        {
            var currentUtc = new DateTime(2016, 1, 1, 0, 0, 0);
            fixture.GetUtcNow = () => currentUtc;
            // Arrange
            const string streamId1 = "stream-1";
            const string streamId2 = "streamId-2";
            await store
                .AppendToStream(streamId1, ExpectedVersion.NoStream, CreateNewStreamMessages(1, 2));
            await store
                .AppendToStream(streamId2, ExpectedVersion.NoStream, CreateNewStreamMessages(1, 2, 3, 4));

            currentUtc += TimeSpan.FromSeconds(60);

            await store
                .AppendToStream(streamId1, ExpectedVersion.Any, CreateNewStreamMessages(5, 6));
            await store
                .AppendToStream(streamId2, ExpectedVersion.Any, CreateNewStreamMessages(5, 6, 7, 8));

            await store
                .SetStreamMetadata(streamId1, maxAge: 30, metadataJson: "meta");
            await store
                .SetStreamMetadata(streamId2, maxAge: 30, metadataJson: "meta");

            // Act
            var messages = await store.ReadAllForwards(Position.Start, 20).ToArrayAsync();

            // Assert
            messages.Count(message => message.StreamId == streamId1).ShouldBe(2);
            messages.Count(message => message.StreamId == streamId2).ShouldBe(4);
        }

        [Fact, Trait("Category", "StreamMetadata")]
        public async Task
            When_streams_have_expired_messages_and_read_all_backwards_then_should_not_get_expired_messages()
        {
            var currentUtc = new DateTime(2016, 1, 1, 0, 0, 0);
            fixture.GetUtcNow = () => currentUtc;

            // Arrange
            const string streamId1 = "stream-1";
            const string streamId2 = "streamId-2";
            await store
                .AppendToStream(streamId1, ExpectedVersion.NoStream, CreateNewStreamMessages(1, 2));
            await store
                .AppendToStream(streamId2, ExpectedVersion.NoStream, CreateNewStreamMessages(1, 2, 3, 4));

            currentUtc += TimeSpan.FromSeconds(60);

            await store
                .AppendToStream(streamId1, ExpectedVersion.Any, CreateNewStreamMessages(5, 6));
            await store
                .AppendToStream(streamId2, ExpectedVersion.Any, CreateNewStreamMessages(5, 6, 7, 8));

            await store
                .SetStreamMetadata(streamId1, maxAge: 30, metadataJson: "meta");
            await store
                .SetStreamMetadata(streamId2, maxAge: 30, metadataJson: "meta");

            // Act
            var messages = await store.ReadAllBackwards(Position.End, 20).ToArrayAsync();

            // Assert
            messages.Count(message => message.StreamId == streamId1).ShouldBe(2);
            messages.Count(message => message.StreamId == streamId2).ShouldBe(4);
        }
    }
}