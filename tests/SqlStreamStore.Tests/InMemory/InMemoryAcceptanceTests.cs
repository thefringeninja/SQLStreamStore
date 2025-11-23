// ReSharper disable once CheckNamespace
namespace SqlStreamStore;

using System.Threading.Tasks;
using Meziantou.Xunit;
using SqlStreamStore.InMemory;
using Xunit.Abstractions;

[EnableParallelization]
public class InMemoryAcceptanceTests(ITestOutputHelper testOutputHelper) : AcceptanceTests(testOutputHelper) {
	protected override Task<IStreamStoreFixture> CreateFixture()
		=> Task.FromResult<IStreamStoreFixture>(new InMemoryStreamStoreFixture(loggerFactory: LoggerFactory));
}
