namespace SqlStreamStore
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Linq;
    using Shouldly;
    using SqlStreamStore.Streams;
    using Xunit;

    public partial class AcceptanceTests
    {
        [Fact]
        public async Task Given_large_message_stream_can_be_read_back_in_pages()
        {
            const int BatchSize = 500;

            var eventsToWrite = CreateNewMessages();

            await store.AppendToStream("stream-1", ExpectedVersion.NoStream, eventsToWrite);

            var result = await store.ReadStreamForwards("stream-1", StreamVersion.Start, BatchSize);
            var count = await result.CountAsync();

            count.ShouldBe(eventsToWrite.Length);
        }

        private static NewStreamMessage[] CreateNewMessages()
        {
            const int largeStreamCount = 7500;
            var eventsToWrite = new List<NewStreamMessage>();
            for(int i = 0; i < largeStreamCount; i++)
            {
                var envelope = new NewStreamMessage(Guid.NewGuid(), $"message-{i}", "{}", $"{i}");

                eventsToWrite.Add(envelope);
            }

            return eventsToWrite.ToArray();
        }
    }
}