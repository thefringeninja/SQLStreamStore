namespace SqlStreamStore
{
    using System.Linq;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using Shouldly;
    using SqlStreamStore.Streams;
    using Xunit;

    public partial class AcceptanceTests
    {
        [Fact, Trait("Category", "ReadAll")]
        public async Task Can_read_all_forwards()
        {
            
            await store.AppendToStream("stream-1", ExpectedVersion.NoStream, CreateNewStreamMessages(1, 2, 3));
            await store.AppendToStream("stream-2", ExpectedVersion.NoStream, CreateNewStreamMessages(4, 5, 6));
            var expectedMessages = new[]
            {
                ExpectedStreamMessage("stream-1", 1, 0, fixture.GetUtcNow()),
                ExpectedStreamMessage("stream-1", 2, 1, fixture.GetUtcNow()),
                ExpectedStreamMessage("stream-1", 3, 2, fixture.GetUtcNow()),
                ExpectedStreamMessage("stream-2", 4, 0, fixture.GetUtcNow()),
                ExpectedStreamMessage("stream-2", 5, 1, fixture.GetUtcNow()),
                ExpectedStreamMessage("stream-2", 6, 2, fixture.GetUtcNow())
            };

            var messages = await store.ReadAllForwards(Position.Start, 4).ToArrayAsync();
            
            messages.Length.ShouldBe(expectedMessages.Length);

            for (int i = 0; i < messages.Length; i++)
            {
                var message = messages[i];
                var expectedMessage = expectedMessages[i];

                message.MessageId.ShouldBe(expectedMessage.MessageId);
                var jsonData = await message.GetJsonData();
                var expectedJsonData = await expectedMessage.GetJsonData();
                JToken.DeepEquals(
                        JObject.Parse(jsonData),
                        JObject.Parse(expectedJsonData))
                    .ShouldBeTrue();
                JToken.DeepEquals(JObject.Parse(message.JsonMetadata), JObject.Parse(expectedMessage.JsonMetadata))
                    .ShouldBeTrue();
                message.StreamId.ShouldBe(expectedMessage.StreamId);
                message.StreamVersion.ShouldBe(expectedMessage.StreamVersion);
                message.Type.ShouldBe(expectedMessage.Type);

                // We don't care about StreamMessage.Position and StreamMessage.Position
                // as they are non-deterministic
            }
        }

        /*[Fact, Trait("Category", "ReadAll")]
        public async Task Can_read_all_forwards_without_prefetch()
        {
            using (var fixture = GetFixture())
            {
                using (var store = await fixture.GetStreamStore())
                {
                    await store.AppendToStream("stream-1", ExpectedVersion.NoStream, CreateNewStreamMessages(1, 2, 3));

                    var page = await store.ReadAllForwards(Position.Start, 4, prefetchJsonData: false);

                    foreach(var streamMessage in page.Messages)
                    {
                        streamMessage.GetJsonData().IsCompleted.ShouldBeFalse();

                        (await streamMessage.GetJsonData()).ShouldNotBeNullOrWhiteSpace();
                    }
                }
            }
        }*/

        [Fact, Trait("Category", "ReadAll")]
        public async Task When_read_without_prefetch_and_stream_is_deleted_then_GetJsonData_should_return_null()
        {
            await store.AppendToStream("stream-1", ExpectedVersion.NoStream, CreateNewStreamMessages(1, 2, 3));

            var messages = await store.ReadAllForwards(Position.Start, 4, prefetchJsonData: false).ToArrayAsync();

            await store.DeleteStream("stream-1");

            foreach (var streamMessage in messages)
            {
                (await streamMessage.GetJsonData()).ShouldBeNull();
            }
        }

        [Fact, Trait("Category", "ReadAll")]
        public async Task Can_read_all_backwards()
        {
            await store.AppendToStream("stream-1", ExpectedVersion.NoStream, CreateNewStreamMessages(1, 2, 3));
            await store.AppendToStream("stream-2", ExpectedVersion.NoStream, CreateNewStreamMessages(4, 5, 6));
            var expectedMessages = new[]
            {
                ExpectedStreamMessage("stream-1", 1, 0, fixture.GetUtcNow()),
                ExpectedStreamMessage("stream-1", 2, 1, fixture.GetUtcNow()),
                ExpectedStreamMessage("stream-1", 3, 2, fixture.GetUtcNow()),
                ExpectedStreamMessage("stream-2", 4, 0, fixture.GetUtcNow()),
                ExpectedStreamMessage("stream-2", 5, 1, fixture.GetUtcNow()),
                ExpectedStreamMessage("stream-2", 6, 2, fixture.GetUtcNow())
            }.Reverse().ToArray();

            var messages = await store.ReadAllBackwards(Position.End, 4).ToArrayAsync();

            messages.Length.ShouldBe(expectedMessages.Length);

            for(int i = 0; i < messages.Length; i++)
            {
                var message = messages[i];
                var expectedMessage = expectedMessages[i];

                message.MessageId.ShouldBe(expectedMessage.MessageId);
                var jsonData = await message.GetJsonData();
                var expectedJsonData = await expectedMessage.GetJsonData();
                JToken.DeepEquals(
                        JObject.Parse(jsonData),
                        JObject.Parse(expectedJsonData))
                    .ShouldBeTrue();
                JToken.DeepEquals(JObject.Parse(message.JsonMetadata), JObject.Parse(expectedMessage.JsonMetadata))
                    .ShouldBeTrue();
                message.StreamId.ShouldBe(expectedMessage.StreamId);
                message.StreamVersion.ShouldBe(expectedMessage.StreamVersion);
                message.Type.ShouldBe(expectedMessage.Type);

                // We don't care about StreamMessage.Position and StreamMessage.Position
                // as they are non-deterministic
            }
        }

        /*[Fact, Trait("Category", "ReadAll")]
        public async Task Can_read_all_backwards_without_prefetch()
        {
            using (var fixture = GetFixture())
            {
                using (var store = await fixture.GetStreamStore())
                {
                    await store.AppendToStream("stream-1", ExpectedVersion.NoStream, CreateNewStreamMessages(1, 2, 3));

                    var page = await store.ReadAllBackwards(Position.End, 4, prefetchJsonData: false);

                    foreach (var streamMessage in page.Messages)
                    {
                        streamMessage.GetJsonData().IsCompleted.ShouldBeFalse();

                        (await streamMessage.GetJsonData()).ShouldNotBeNullOrWhiteSpace();
                    }
                }
            }
        }*/

        [Theory, Trait("Category", "ReadAll")]
        [InlineData(3, 0, 3, 3, 0, 3)]  // Read entire store
        [InlineData(3, 0, 4, 3, 0, 3)]  // Read entire store
        [InlineData(3, 0, 2, 2, 0, 2)]
        [InlineData(3, 1, 2, 2, 1, 3)]
        [InlineData(3, 2, 1, 1, 2, 3)]
        [InlineData(3, 3, 1, 0, 3, 3)]
        public async Task When_read_all_forwards(
            int numberOfSeedMessages,
            int fromPosition,
            int maxCount,
            int expectedCount,
            int expectedFromPosition,
            int expectedNextPosition)
        {
            await store.AppendToStream(
                "stream-1",
                ExpectedVersion.NoStream,
                CreateNewStreamMessageSequence(1, numberOfSeedMessages));

            var messages = await store.ReadAllForwards(fromPosition, maxCount).Take(maxCount).ToArrayAsync();

            messages.Length.ShouldBe(expectedCount);
        }

        [Theory, Trait("Category", "ReadAll")]
        [InlineData(3, -1, 1, 1, 2, 1)] // -1 is Position.End
        [InlineData(3, 2, 1, 1, 2, 1)]
        [InlineData(3, 1, 1, 1, 1, 0)]
        [InlineData(3, 0, 1, 1, 0, 0)]
        [InlineData(3, -1, 3, 3, 2, 0)] // Read entire store
        [InlineData(3, -1, 4, 3, 2, 0)] // Read entire store
        [InlineData(0, -1, 1, 0, 0, 0)]
        public async Task When_read_all_backwards(
            int numberOfSeedMessages,
            int fromPosition,
            int maxCount,
            int expectedCount,
            int expectedFromPosition,
            int expectedNextPosition)
        {
            if(numberOfSeedMessages > 0)
            {
                await store.AppendToStream(
                    "stream-1",
                    ExpectedVersion.NoStream,
                    CreateNewStreamMessageSequence(1, numberOfSeedMessages));
            }

            var messages = await store.ReadAllBackwards(fromPosition, maxCount).Take(expectedCount).ToArrayAsync();

            messages.Length.ShouldBe(expectedCount);            
        }

        [Theory, Trait("Category", "ReadAll")]
        [InlineData("stream/1")]
        [InlineData("stream%1")]
        public async Task when_read_all_forwards_with_url_encodable_stream(string streamId)
        {
            await store.AppendToStream(streamId, ExpectedVersion.NoStream, CreateNewStreamMessages(1));

            var result = await store.ReadAllForwards(Position.Start, 1).ToArrayAsync();
            
            Assert.Equal(streamId, result[0].StreamId);
        }

        [Theory, Trait("Category", "ReadAll")]
        [InlineData("stream/1")]
        [InlineData("stream%1")]
        public async Task when_read_all_backwards_with_url_encodable_stream(string streamId)
        {
            await store.AppendToStream(streamId, ExpectedVersion.NoStream, CreateNewStreamMessages(1));

            var result = await store.ReadAllBackwards(Position.End, 1).ToArrayAsync();
            
            Assert.Equal(streamId, result[0].StreamId);
        }
    }
}
