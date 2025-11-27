namespace SqlStreamStore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Shouldly;
using SqlStreamStore.Streams;
using Xunit;
using Xunit.Abstractions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

public abstract partial class AcceptanceTests : IAsyncLifetime {
	protected ILogger Logger { get; }
	private const string DefaultJsonData = @"{ ""data"": ""data"" }";
	private const string DefaultJsonMetadata = @"{ ""meta"": ""data"" }";

	protected ILoggerFactory LoggerFactory { get; }

	protected AcceptanceTests(ITestOutputHelper testOutputHelper) {
		LoggerFactory = new SerilogLoggerFactory(new LoggerConfiguration()
			.MinimumLevel.Debug()
			.Enrich.FromLogContext()
			.WriteTo.TestOutput(testOutputHelper)
			.CreateLogger());
		Logger = LoggerFactory.CreateLogger<AcceptanceTests>();
	}

	public async Task InitializeAsync() {
		Fixture = await CreateFixture();
	}

	private IStreamStore Store => Fixture.Store;

	protected IStreamStoreFixture Fixture { get; private set; } = null!;

	public Task DisposeAsync() {
		Fixture.Dispose();
		return Task.CompletedTask;
	}

	protected abstract Task<IStreamStoreFixture> CreateFixture();

	[Fact]
	public async Task When_dispose_and_read_then_should_throw() {
		Store.Dispose();

		Func<Task> act = () => Store.ReadAllForwards(Position.Start, 10);

		await act.ShouldThrowAsync<ObjectDisposedException>();
	}

	[Fact]
	public void Can_dispose_more_than_once() {
		Store.Dispose();

		Action act = Store.Dispose;

		act.ShouldNotThrow();
	}

	public static NewStreamMessage[] CreateNewStreamMessages(params int[] messageNumbers) {
		return CreateNewStreamMessages(DefaultJsonData, messageNumbers);
	}

	private static NewStreamMessage[] CreateNewStreamMessages(string jsonData, params int[] messageNumbers) {
		return messageNumbers
			.Select(number => {
				var id = Guid.Parse("00000000-0000-0000-0000-" + number.ToString().PadLeft(12, '0'));
				return new NewStreamMessage(id, "type", jsonData, DefaultJsonMetadata);
			})
			.ToArray();
	}

	public static NewStreamMessage[] CreateNewStreamMessageSequence(int startId, int count) {
		var messages = new List<NewStreamMessage>();
		for (int i = 0; i < count; i++) {
			var messageNumber = startId + i;
			var messageId = Guid.Parse("00000000-0000-0000-0000-" + messageNumber.ToString().PadLeft(12, '0'));
			var newStreamMessage = new NewStreamMessage(messageId, "type", DefaultJsonData, DefaultJsonMetadata);
			messages.Add(newStreamMessage);
		}
		return messages.ToArray();
	}

	public static StreamMessage ExpectedStreamMessage(
		string streamId,
		int messageNumber,
		int sequenceNumber,
		DateTime created) {
		var id = Guid.Parse("00000000-0000-0000-0000-" + messageNumber.ToString().PadLeft(12, '0'));
		return new StreamMessage(streamId, id, sequenceNumber, 0, created, "type", DefaultJsonMetadata, DefaultJsonData);
	}
}