namespace System.Linq
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using SqlStreamStore.Streams;

    internal static class AsyncEnumerableExtensions
    {
        public static async Task<StreamMessage[]> ToArrayAsync(this Task<ReadStreamResult> itemsTask)
            => await (await itemsTask).ToArrayAsync();

        public static async Task<StreamMessage[]> ToArrayAsync(this Task<IAsyncEnumerable<StreamMessage>> itemsTask)
            => await (await itemsTask).ToArrayAsync();
    }
}