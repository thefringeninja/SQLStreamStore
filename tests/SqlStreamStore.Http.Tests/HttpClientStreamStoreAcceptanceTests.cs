namespace SqlStreamStore;

using System.Threading.Tasks;
using Meziantou.Xunit;
using Xunit.Abstractions;

[EnableParallelization]
public class HttpClientStreamStoreAcceptanceTests(ITestOutputHelper testOutputHelper)
	: AcceptanceTests(testOutputHelper) {
	protected override Task<IStreamStoreFixture> CreateFixture()
		=> Task.FromResult<IStreamStoreFixture>(new HttpClientStreamStoreFixture(LoggerFactory));
}
