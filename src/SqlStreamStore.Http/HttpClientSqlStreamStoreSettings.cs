namespace SqlStreamStore;

using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlStreamStore.Infrastructure;
using SqlStreamStore.Subscriptions;

public class HttpClientSqlStreamStoreSettings(ILoggerFactory? loggerFactory = null) {
	public ILoggerFactory? LoggerFactory { get; } = loggerFactory;

	private static readonly Lazy<Func<HttpClient>> s_defaultHttpClientFactory = new(() => {
		var serviceProvider = new ServiceCollection()
			.AddHttpClient(nameof(HttpClientSqlStreamStore)).Services
			.BuildServiceProvider();

		return serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient;
	});

	/// <summary>
	/// The root uri of the server. Must end with "/".
	/// </summary>
	public Uri BaseAddress { get; set; } = new UriBuilder().Uri;

	public CreateStreamStoreNotifier CreateStreamStoreNotifier { get; set; } =
		store => new PollingStreamStoreNotifier(store, loggerFactory?.CreateLogger<HttpClientSqlStreamStore>());

	/// <summary>
	///     A delegate to return the current UTC now. Used in testing to
	///     control timestamps and time related operations.
	/// </summary>
	public GetUtcNow GetUtcNow { get; set; } = SystemClock.GetUtcNow;

	/// <summary>
	///     The log name used for the any log messages.
	/// </summary>
	public string LogName { get; set; } = nameof(HttpClientSqlStreamStore);

	/// <summary>
	/// The HttpClient Factory
	/// </summary>
	public Func<HttpClient> CreateHttpClient { get; set; } = s_defaultHttpClientFactory.Value;
}