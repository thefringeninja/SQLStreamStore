namespace SqlStreamStore;

using System.Threading.Tasks;
using Meziantou.Xunit;
using Xunit.Abstractions;

[DisableParallelization]
public class SqliteStreamStoreAcceptanceTests(ITestOutputHelper testOutputHelper) : AcceptanceTests(testOutputHelper) {
	protected override async Task<IStreamStoreFixture> CreateFixture() {
		var fixture = new SqliteStreamStoreFixture(LoggerFactory);
		await fixture.Prepare();
		return fixture;
	}
}
