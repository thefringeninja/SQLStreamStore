namespace SqlStreamStore.HAL.Tests;

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using SqlStreamStore.Streams;
using Xunit.Abstractions;

internal class SqlStreamStoreHalMiddlewareFixture : IDisposable {
	public IStreamStore StreamStore { get; }
	public HttpClient HttpClient { get; }

	private readonly TestServer _server;
	private readonly WebApplication _app;

	public SqlStreamStoreHalMiddlewareFixture(ITestOutputHelper output, bool followRedirects = false) {
		var serilogLogger = new LoggerConfiguration()
			.MinimumLevel.Debug()
			.Enrich.FromLogContext()
			.WriteTo.TestOutput(output)
			.CreateLogger();
		StreamStore = new InMemoryStreamStore(logger: new SerilogLoggerFactory(serilogLogger).CreateLogger<InMemoryStreamStore>());

		var builder = WebApplication.CreateBuilder();
		builder.Logging.AddSerilog(serilogLogger);
		builder.Services.AddSqlStreamStoreHal();
		builder.WebHost.UseTestServer();
		_app = builder.Build();
		_app.UseSqlStreamStoreHal(StreamStore);
		_server = _app.GetTestServer();
		_app.StartAsync().Wait();

		var handler = _server.CreateHandler();
		if (followRedirects) {
			handler = new RedirectingHandler {
				InnerHandler = handler
			};
		}

		HttpClient = new HttpClient(handler) { BaseAddress = new UriBuilder().Uri };
		HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/hal+json"));
	}

	public void Dispose() {
		StreamStore?.Dispose();
		HttpClient?.Dispose();
		_server?.Dispose();
		_app.DisposeAsync().GetAwaiter().GetResult();
	}

	public Task<AppendResult> WriteNMessages(string streamId, int n)
		=> StreamStore.AppendToStream(
			streamId,
			ExpectedVersion.Any,
			Enumerable.Range(0, n)
				.Select(_ => new NewStreamMessage(Guid.NewGuid(), "type", "{}", "{}"))
				.ToArray());
}