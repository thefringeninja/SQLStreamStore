using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using SqlStreamStore;
using SqlStreamStore.HAL;
using SqlStreamStore.HAL.DevServer;
using SqlStreamStore.Streams;

var random = new Random();
using var cts = new CancellationTokenSource();
var configuration = new ConfigurationBuilder()
	.AddEnvironmentVariables()
	.AddCommandLine(args)
	.Build();

var interactive = configuration.GetValue<bool>("interactive");
var useCanonicalUrls = configuration.GetValue<bool>("canonical");

var builder = WebApplication.CreateSlimBuilder(args);

builder.Logging.AddSerilog(new LoggerConfiguration()
	.MinimumLevel.Debug()
	.Enrich.FromLogContext()
	.WriteTo.Console()
	.CreateLogger());

builder.Services
	.AddResponseCompression(options => options.MimeTypes = new[] { "application/hal+json" })
	.AddSqlStreamStoreHal();

var app = builder.Build();

using var streamStore = await SqlStreamStoreFactory.Create(app.Services.GetRequiredService<ILoggerFactory>());

app.UseResponseCompression()
	.Use(VaryAccept)
	.UseSqlStreamStoreBrowser()
	.UseSqlStreamStoreHal(streamStore,
		new SqlStreamStoreMiddlewareOptions {
			UseCanonicalUrls = useCanonicalUrls,
			ServerAssembly = typeof(Program).Assembly
		});

Task VaryAccept(HttpContext context, Func<Task> next) {
	context.Response.OnStarting(Vary);

	return next();

	Task Vary() {
		context.Response.Headers.AppendCommaSeparatedValues("Vary", "Accept");

		return Task.CompletedTask;
	}
}

try {
	var serverTask = app.StartAsync(cts.Token);

	if (interactive) {
		Log.Warning("Running interactively.");
		DisplayMenu(streamStore);
	}

	await serverTask;

	await app.StopAsync(cts.Token);

	return 0;
} catch (Exception ex) {
	Log.Fatal(ex, "Host terminated unexpectedly.");
	return 1;
} finally {
	Log.CloseAndFlush();
}


void DisplayMenu(IStreamStore streamStore) {
	while (true) {
		Console.WriteLine("Press w to write 10 messages each to 100 streams");
		Console.WriteLine("Press t to write 100 messages each to 10 streams");
		Console.WriteLine("Press ESC to exit");

		var key = Console.ReadKey();

		switch (key.Key) {
			case ConsoleKey.Escape:
				return;
			case ConsoleKey.W:
				Write(streamStore, 10, 100);
				break;
			case ConsoleKey.T:
				Write(streamStore, 100, 10);
				break;
			default:
				Console.WriteLine("Computer says no");
				break;
		}
	}
}

void Write(IStreamStore streamStore, int messageCount, int streamCount) {
	var streams = Enumerable.Range(0, streamCount).Select(_ => $"test-{Guid.NewGuid():n}").ToList();

	Task.Run(() => Task.WhenAll(
		from streamId in streams
		select streamStore.AppendToStream(
			streamId,
			ExpectedVersion.NoStream,
			GenerateMessages(messageCount))));
}

NewStreamMessage[] GenerateMessages(int messageCount)
	=> Enumerable.Range(0, messageCount)
		.Select(_ => new NewStreamMessage(
			Guid.NewGuid(),
			"test",
			$@"{{ ""foo"": ""{Guid.NewGuid()}"", ""baz"": {{  }}, ""qux"": [ {string.Join(", ",
					Enumerable
						.Range(0, messageCount).Select(max => random.Next(max)))} ] }}",
			"{}"))
		.ToArray();
