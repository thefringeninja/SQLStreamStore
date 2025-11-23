namespace SqlStreamStore;

using System;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlStreamStore.HAL;
using SqlStreamStore.Infrastructure;

public class HttpClientStreamStoreFixture : IStreamStoreFixture {
	private readonly InMemoryStreamStore _inMemoryStreamStore;
	private readonly TestServer _server;

	public HttpClientStreamStoreFixture(ILoggerFactory loggerFactory) {
		_inMemoryStreamStore = new InMemoryStreamStore(() => GetUtcNow(), loggerFactory.CreateLogger<InMemoryStreamStore>());

		var random = new Random();

		var segments = Enumerable.Range(0, random.Next(1, 3)).Select(_ => Guid.NewGuid()).ToArray();
		var basePath = $"/{string.Join("/", segments)}";

		var builder = WebApplication.CreateBuilder();
		builder.Services.AddSingleton<ILoggerFactory>(loggerFactory);
		builder.Services.AddSqlStreamStoreHal();
		builder.WebHost.UseTestServer();
		var app = builder.Build();
		app.Map(basePath, inner => inner.UseSqlStreamStoreHal(_inMemoryStreamStore));

		_server = app.GetTestServer();

		app.StartAsync().Wait();

		var handler = new RedirectingHandler {
			InnerHandler = _server.CreateHandler()
		};

		Store = new HttpClientSqlStreamStore(
			new HttpClientSqlStreamStoreSettings {
				GetUtcNow = () => GetUtcNow(),
				BaseAddress = new UriBuilder {
					Path = basePath.Length == 1 ? basePath : $"{basePath}/"
				}.Uri,
				CreateHttpClient = () => new HttpClient(handler, false)
			});
	}

	public void Dispose() {
		Store.Dispose();
		_server.Dispose();
		_inMemoryStreamStore.Dispose();
	}

	public IStreamStore Store { get; }

	public GetUtcNow GetUtcNow { get; set; } = SystemClock.GetUtcNow;

	public long MinPosition { get; set; } = 0;

	public int MaxSubscriptionCount { get; set; } = 500;

	public bool DisableDeletionTracking {
		get => throw new NotSupportedException();
		set => throw new NotSupportedException();
	}
}