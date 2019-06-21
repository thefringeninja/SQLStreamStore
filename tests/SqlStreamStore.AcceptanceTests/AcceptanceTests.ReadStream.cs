﻿namespace SqlStreamStore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using Shouldly;
    using SqlStreamStore.Infrastructure;
    using SqlStreamStore.Streams;
    using Xunit;

    public partial class AcceptanceTests
    {
        [Theory, Trait("Category", "ReadStream")]
        [MemberData(nameof(GetReadStreamForwardsTheories))]
        public async Task Can_read_streams_forwards_with_prefetch(ReadStreamTheory theory)
        {
            await store.AppendToStream("stream-1", ExpectedVersion.NoStream, CreateNewStreamMessages(1, 2, 3));
            await store.AppendToStream("stream-2", ExpectedVersion.NoStream, CreateNewStreamMessages(4, 5, 6));

            var result = await store.ReadStreamForwards(theory.StreamId, theory.Start, theory.PageSize);

            var expectedStreamMessagesPage = theory.ExpectedReadStreamPage;
            var expectedMessages = theory.ExpectedReadStreamPage.Messages.ToArray();

            result.LastStreamVersion.ShouldBe(expectedStreamMessagesPage.LastStreamVersion);
            result.Status.ShouldBe(expectedStreamMessagesPage.Status);

            var messages = await result.Take(theory.PageSize).ToArrayAsync();
            messages.Length.ShouldBe(expectedStreamMessagesPage.Messages.Length);

            for(int i = 0; i < messages.Length; i++)
            {
                var message = messages.ToArray()[i];
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

        /*[Theory, Trait("Category", "ReadStream")]
        [MemberData(nameof(GetReadStreamForwardsTheories))]
        public async Task Can_read_streams_forwards_without_prefetch(ReadStreamTheory theory)
        {
            using (var fixture = GetFixture())
            {
                using (var store = await fixture.GetStreamStore())
                {
                    await store.AppendToStream("stream-1", ExpectedVersion.NoStream, CreateNewStreamMessages(1, 2, 3));
                    await store.AppendToStream("stream-2", ExpectedVersion.NoStream, CreateNewStreamMessages(4, 5, 6));

                    var streamMessagesPage =
                        await store.ReadStreamForwards(theory.StreamId, theory.Start, theory.PageSize, false);

                    var expectedStreamMessagesPage = theory.ExpectedReadStreamPage;
                    var expectedMessages = theory.ExpectedReadStreamPage.Messages.ToArray();

                    streamMessagesPage.FromStreamVersion.ShouldBe(expectedStreamMessagesPage.FromStreamVersion);
                    streamMessagesPage.LastStreamVersion.ShouldBe(expectedStreamMessagesPage.LastStreamVersion);
                    streamMessagesPage.NextStreamVersion.ShouldBe(expectedStreamMessagesPage.NextStreamVersion);
                    streamMessagesPage.ReadDirection.ShouldBe(expectedStreamMessagesPage.ReadDirection);
                    streamMessagesPage.IsEnd.ShouldBe(expectedStreamMessagesPage.IsEnd);
                    streamMessagesPage.Status.ShouldBe(expectedStreamMessagesPage.Status);
                    streamMessagesPage.StreamId.ShouldBe(expectedStreamMessagesPage.StreamId);
                    streamMessagesPage.Messages.Length.ShouldBe(expectedStreamMessagesPage.Messages.Length);

                    for (int i = 0; i < streamMessagesPage.Messages.Length; i++)
                    {
                        var message = streamMessagesPage.Messages.ToArray()[i];
                        var expectedMessage = expectedMessages[i];

                        message.MessageId.ShouldBe(expectedMessage.MessageId);
                        (await message.GetJsonData()).ShouldBe(await expectedMessage.GetJsonData());
                        message.JsonMetadata.ShouldBe(expectedMessage.JsonMetadata);
                        message.StreamId.ShouldBe(expectedMessage.StreamId);
                        message.StreamVersion.ShouldBe(expectedMessage.StreamVersion);
                        message.Type.ShouldBe(expectedMessage.Type);

                        // We don't care about StreamMessage.Position and StreamMessage.Position
                        // as they are non-deterministic
                    }
                }
            }
        }*/

        /*[Fact, Trait("Category", "ReadStream")]
        public async Task Can_read_whole_stream_forwards_without_prefetch()
        {
            using (var fixture = GetFixture())
            {
                using (var store = await fixture.GetStreamStore())
                {
                    await store.AppendToStream("stream-1", ExpectedVersion.NoStream, CreateNewStreamMessages(1, 2, 3));

                    var page = await store.ReadStreamForwards("stream-1", StreamVersion.Start, 5, prefetchJsonData: false);

                    foreach (var streamMessage in page.Messages)
                    {
                        streamMessage.GetJsonData().IsCompleted.ShouldBeFalse();

                        (await streamMessage.GetJsonData()).ShouldNotBeNullOrWhiteSpace();
                    }
                }
            }
        }*/

        [Fact, Trait("Category", "ReadStream")]
        public async Task Can_read_all_messages()
        {
            await store.AppendToStream("stream-1", ExpectedVersion.NoStream, CreateNewStreamMessages(1, 2, 3));

            var result = await store.ReadStreamForwards("stream-1", StreamVersion.Start, int.MaxValue).ToArrayAsync();

            result.Length.ShouldBe(3);
        }

        [Theory, Trait("Category", "ReadStream")]
        [MemberData(nameof(GetReadStreamBackwardsTheories))]
        public async Task Can_read_streams_backwards_with_prefetch(ReadStreamTheory theory)
        {
            await store.AppendToStream("stream-1", ExpectedVersion.NoStream, CreateNewStreamMessages(1, 2, 3));
            await store.AppendToStream("stream-2", ExpectedVersion.NoStream, CreateNewStreamMessages(4, 5, 6));

            var result =
                await store.ReadStreamBackwards(theory.StreamId, theory.Start, theory.PageSize);

            var expectedStreamMessagesPage = theory.ExpectedReadStreamPage;
            var expectedMessages = theory.ExpectedReadStreamPage.Messages.ToArray();

            result.LastStreamVersion.ShouldBe(expectedStreamMessagesPage.LastStreamVersion);
            result.Status.ShouldBe(expectedStreamMessagesPage.Status);

            var messages = await result.Take(theory.PageSize).ToArrayAsync();

            messages.Length.ShouldBe(expectedStreamMessagesPage.Messages.Length);

            for(int i = 0; i < messages.Length; i++)
            {
                var streamMessage = messages.ToArray()[i];
                var expectedMessage = expectedMessages[i];

                streamMessage.MessageId.ShouldBe(expectedMessage.MessageId);
                var jsonData = await streamMessage.GetJsonData();
                var expectedJsonData = await expectedMessage.GetJsonData();
                JToken.DeepEquals(
                        JObject.Parse(jsonData),
                        JObject.Parse(expectedJsonData))
                    .ShouldBeTrue();
                JToken.DeepEquals(JObject.Parse(streamMessage.JsonMetadata),
                        JObject.Parse(expectedMessage.JsonMetadata))
                    .ShouldBeTrue();
                streamMessage.StreamId.ShouldBe(expectedMessage.StreamId);
                streamMessage.StreamVersion.ShouldBe(expectedMessage.StreamVersion);
                streamMessage.Type.ShouldBe(expectedMessage.Type);

                // We don't care about StreamMessage.Position and StreamMessage.Position
                // as they are non-deterministic
            }
        }

        /*[Theory, Trait("Category", "ReadStream")]
        [MemberData(nameof(GetReadStreamBackwardsTheories))]
        public async Task Can_read_streams_backwards_without_prefetch(ReadStreamTheory theory)
        {
            using (var fixture = GetFixture())
            {
                using (var store = await fixture.GetStreamStore())
                {
                    await store.AppendToStream("stream-1", ExpectedVersion.NoStream, CreateNewStreamMessages(1, 2, 3));
                    await store.AppendToStream("stream-2", ExpectedVersion.NoStream, CreateNewStreamMessages(4, 5, 6));

                    var streamMessagesPage =
                        await store.ReadStreamBackwards(theory.StreamId, theory.Start, theory.PageSize, false);

                    var expectedStreamMessagesPage = theory.ExpectedReadStreamPage;
                    var expectedMessages = theory.ExpectedReadStreamPage.Messages.ToArray();

                    streamMessagesPage.FromStreamVersion.ShouldBe(expectedStreamMessagesPage.FromStreamVersion);
                    streamMessagesPage.LastStreamVersion.ShouldBe(expectedStreamMessagesPage.LastStreamVersion);
                    streamMessagesPage.NextStreamVersion.ShouldBe(expectedStreamMessagesPage.NextStreamVersion);
                    streamMessagesPage.ReadDirection.ShouldBe(expectedStreamMessagesPage.ReadDirection);
                    streamMessagesPage.IsEnd.ShouldBe(expectedStreamMessagesPage.IsEnd);
                    streamMessagesPage.Status.ShouldBe(expectedStreamMessagesPage.Status);
                    streamMessagesPage.StreamId.ShouldBe(expectedStreamMessagesPage.StreamId);
                    streamMessagesPage.Messages.Length.ShouldBe(expectedStreamMessagesPage.Messages.Length);

                    for (int i = 0; i < streamMessagesPage.Messages.Length; i++)
                    {
                        var streamMessage = streamMessagesPage.Messages.ToArray()[i];
                        var expectedMessage = expectedMessages[i];

                        streamMessage.MessageId.ShouldBe(expectedMessage.MessageId);
                        (await streamMessage.GetJsonData()).ShouldBe(await expectedMessage.GetJsonData());
                        streamMessage.JsonMetadata.ShouldBe(expectedMessage.JsonMetadata);
                        streamMessage.StreamId.ShouldBe(expectedMessage.StreamId);
                        streamMessage.StreamVersion.ShouldBe(expectedMessage.StreamVersion);
                        streamMessage.Type.ShouldBe(expectedMessage.Type);

                        // We don't care about StreamMessage.Position and StreamMessage.Position
                        // as they are non-deterministic
                    }
                }
            }
        }*/

        /*[Fact, Trait("Category", "ReadStream")]
        public async Task Can_read_stream_backwards_without_prefetch()
        {
            using (var fixture = GetFixture())
            {
                using (var store = await fixture.GetStreamStore())
                {
                    await store.AppendToStream("stream-1", ExpectedVersion.NoStream, CreateNewStreamMessages(1, 2, 3));

                    var page = await store.ReadStreamBackwards("stream-1", StreamVersion.End, 5, prefetchJsonData: false);

                    foreach (var streamMessage in page.Messages)
                    {
                        streamMessage.GetJsonData().IsCompleted.ShouldBeFalse();

                        (await streamMessage.GetJsonData()).ShouldNotBeNullOrWhiteSpace();
                    }
                }
            }
        }*/

        [Theory, Trait("Category", "ReadStream")]
        [InlineData(true), InlineData(false)]
        public async Task Can_read_empty_stream_backwards(bool prefetch)
        {
            await store.AppendToStream("stream-1", ExpectedVersion.NoStream, CreateNewStreamMessages());

            var result = await store.ReadStreamBackwards("stream-1", StreamVersion.End, 1, prefetch);
            var count = await result.CountAsync();

            result.Status.ShouldBe(PageReadStatus.Success);
            count.ShouldBe(0);
            result.LastStreamVersion.ShouldBe(StreamVersion.End);
        }

        [Theory, Trait("Category", "ReadStream")]
        [InlineData(true), InlineData(false)]
        public async Task Can_read_empty_stream_forwards(bool prefetch)
        {
            await store.AppendToStream("stream-1", ExpectedVersion.NoStream, CreateNewStreamMessages());

            var result = await store.ReadStreamForwards("stream-1", StreamVersion.Start, 1, prefetch);
            result.Status.ShouldBe(PageReadStatus.Success);

            var count = await result.CountAsync();

            result.Status.ShouldBe(PageReadStatus.Success);
            count.ShouldBe(0);
            result.LastStreamVersion.ShouldBe(StreamVersion.End);
        }

        [Fact, Trait("Category", "ReadStream")]
        public async Task When_read_non_exist_stream_forwards_then_should_get_StreamNotFound()
        {
            var streamMessagesPage =
                await store.ReadStreamForwards("stream-does-not-exist", StreamVersion.Start, 1);

            streamMessagesPage.Status.ShouldBe(PageReadStatus.StreamNotFound);
        }

        [Fact, Trait("Category", "ReadStream")]
        public async Task When_read_non_exist_stream_backwards_then_should_get_StreamNotFound()
        {
            var streamMessagesPage =
                await store.ReadStreamBackwards("stream-does-not-exist", StreamVersion.End, 1);

            streamMessagesPage.Status.ShouldBe(PageReadStatus.StreamNotFound);
        }

        [Fact, Trait("Category", "ReadStream")]
        public async Task When_read_deleted_stream_forwards_then_should_get_StreamNotFound()
        {
            await store.AppendToStream("stream-1", ExpectedVersion.NoStream, CreateNewStreamMessages(1, 2, 3));
            await store.DeleteStream("stream-1");

            var streamMessagesPage =
                await store.ReadStreamForwards("stream-1", StreamVersion.Start, 1);

            streamMessagesPage.Status.ShouldBe(PageReadStatus.StreamNotFound);
        }

        [Fact, Trait("Category", "ReadStream")]
        public async Task When_read_deleted_stream_backwards_then_should_get_StreamNotFound()
        {
            await store.AppendToStream("stream-1", ExpectedVersion.NoStream, CreateNewStreamMessages(1, 2, 3));
            await store.DeleteStream("stream-1");

            var streamMessagesPage =
                await store.ReadStreamBackwards("stream-1", StreamVersion.Start, 1);

            streamMessagesPage.Status.ShouldBe(PageReadStatus.StreamNotFound);
        }

        [Theory, InlineData(0), InlineData(1)]
        public async Task Can_read_stream_backwards_starting_past_end_of_stream(int fromVersionInclusive)
        {
            await store.AppendToStream("stream-1", ExpectedVersion.NoStream, Array.Empty<NewStreamMessage>());

            var result = await store.ReadStreamBackwards("stream-1", fromVersionInclusive, 1);
            var count = await result.CountAsync();

            result.Status.ShouldBe(PageReadStatus.Success);
            count.ShouldBe(0);
        }

        [Theory, Trait("Category", "ReadStream")]
        [InlineData("stream/1")]
        [InlineData("stream%1")]
        public async Task when_read_stream_forwards_with_url_encodable_stream(string streamId)
        {
            await store.AppendToStream(streamId, ExpectedVersion.NoStream, CreateNewStreamMessages(1));

            var result = await store.ReadStreamForwards(streamId, StreamVersion.Start, 1);
            var message = await result.FirstAsync();

            Assert.Equal(streamId, message.StreamId);
        }

        [Theory, Trait("Category", "ReadStream")]
        [InlineData("stream/1")]
        [InlineData("stream%1")]
        public async Task when_read_stream_backwards_with_url_encodable_stream(string streamId)
        {
            await store.AppendToStream(streamId, ExpectedVersion.NoStream, CreateNewStreamMessages(1));

            var result = await store.ReadStreamBackwards(streamId, StreamVersion.End, 1);
            var message = await result.FirstAsync();

            Assert.Equal(streamId, message.StreamId);
        }

        // ReSharper disable once UnusedMethodReturnValue.Global
        public static IEnumerable<object[]> GetReadStreamForwardsTheories()
        {
            var theories = new[]
            {
                new ReadStreamTheory("stream-1",
                    StreamVersion.Start,
                    2,
                    new ReadStreamPage("stream-1",
                        PageReadStatus.Success,
                        0,
                        2,
                        2,
                        -1,
                        ReadDirection.Forward,
                        false,
                        messages:
                        new[]
                        {
                            ExpectedStreamMessage("stream-1", 1, 0, SystemClock.GetUtcNow()),
                            ExpectedStreamMessage("stream-1", 2, 1, SystemClock.GetUtcNow())
                        })),

                new ReadStreamTheory("not-exist",
                    1,
                    2,
                    new ReadStreamPage("not-exist",
                        PageReadStatus.StreamNotFound,
                        1,
                        -1,
                        -1,
                        -1,
                        ReadDirection.Forward,
                        true)),

                new ReadStreamTheory("stream-2",
                    1,
                    2,
                    new ReadStreamPage("stream-2",
                        PageReadStatus.Success,
                        1,
                        3,
                        2,
                        -1,
                        ReadDirection.Forward,
                        true,
                        messages:
                        new[]
                        {
                            ExpectedStreamMessage("stream-2", 5, 1, SystemClock.GetUtcNow()),
                            ExpectedStreamMessage("stream-2", 6, 2, SystemClock.GetUtcNow())
                        }))
            };

            return theories.Select(t => new object[] { t });
        }

        public static IEnumerable<object[]> GetReadStreamBackwardsTheories()
        {
            var theories = new[]
            {
                new ReadStreamTheory("stream-1",
                    StreamVersion.End,
                    1,
                    new ReadStreamPage("stream-1",
                        PageReadStatus.Success,
                        -1,
                        1,
                        2,
                        -1,
                        ReadDirection.Backward,
                        false,
                        messages:
                        new[]
                        {
                            ExpectedStreamMessage("stream-1", 3, 2, SystemClock.GetUtcNow())
                        })),

                new ReadStreamTheory("stream-1",
                    StreamVersion.End,
                    2,
                    new ReadStreamPage("stream-1",
                        PageReadStatus.Success,
                        -1,
                        0,
                        2,
                        -1,
                        ReadDirection.Backward,
                        false,
                        messages:
                        new[]
                        {
                            ExpectedStreamMessage("stream-1", 3, 2, SystemClock.GetUtcNow()),
                            ExpectedStreamMessage("stream-1", 2, 1, SystemClock.GetUtcNow())
                        })),

                new ReadStreamTheory("stream-1",
                    StreamVersion.End,
                    4,
                    new ReadStreamPage("stream-1",
                        PageReadStatus.Success,
                        -1,
                        -1,
                        2,
                        -1,
                        ReadDirection.Backward,
                        true,
                        messages:
                        new[]
                        {
                            ExpectedStreamMessage("stream-1", 3, 2, SystemClock.GetUtcNow()),
                            ExpectedStreamMessage("stream-1", 2, 1, SystemClock.GetUtcNow()),
                            ExpectedStreamMessage("stream-1", 1, 0, SystemClock.GetUtcNow())
                        }))
            };

            return theories.Select(t => new object[] { t });
        }

        public class ReadStreamTheory
        {
            public readonly string StreamId;
            public readonly int Start;
            public readonly int PageSize;
            public readonly ReadStreamPage ExpectedReadStreamPage;

            public ReadStreamTheory(
                string streamId,
                int start,
                int pageSize,
                ReadStreamPage expectedReadStreamPage)
            {
                StreamId = streamId;
                Start = start;
                PageSize = pageSize;
                ExpectedReadStreamPage = expectedReadStreamPage;
            }
        }
    }
}