namespace SqlStreamStore.Infrastructure;

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

public class TaskQueueTests {
	private readonly ILogger _logger;

	public TaskQueueTests(ITestOutputHelper testOutputHelper) {
		_logger = new SerilogLoggerFactory(new LoggerConfiguration()
			.MinimumLevel.Debug()
			.Enrich.FromLogContext()
			.WriteTo.TestOutput(testOutputHelper)
			.CreateLogger()).CreateLogger<TaskQueueTests>();
	}

	[Fact]
	public async Task Enqueued_tasks_should_be_executed() {
		using (var taskQueue = new TaskQueue()) {
			var tasks = new ConcurrentBag<Task>();

			for (int i = 0; i < 250; i++) {
				tasks.Add(taskQueue.Enqueue(() => { }));
			}

			await Task.WhenAll(tasks);
		}
	}

	[Fact]
	public async Task Multi_threaded_enqueued_tasks_should_be_executed() {
		using (var taskQueue = new TaskQueue()) {
			var tasks = new ConcurrentBag<Task>();

			Parallel.For(0,
				250,
				i => {
					int j = i;
					var task = taskQueue.Enqueue(() => {
						_logger.LogInformation("{count}", j);
					});
					tasks.Add(task);
				});

			await Task.WhenAll(tasks);
		}
	}

	[Fact]
	public void When_disposed_then_enqueued_task_should_be_cancelled() {
		var taskQueue = new TaskQueue();
		taskQueue.Dispose();

		var task = taskQueue.Enqueue(() => { });

		task.IsCanceled.ShouldBeTrue();
	}

	[Fact]
	public async Task When_enqueued_function_throws_then_should_propagate_exception() {
		using (var taskQueue = new TaskQueue()) {
			var task = taskQueue.Enqueue(() => {
				throw new InvalidOperationException();
			});

			Func<Task> act = async () => await task;

			await act.ShouldThrowAsync<InvalidOperationException>();
		}
	}

	[Fact]
	public async Task When_enqueued_function_cancels_then_should_propagate_exception() {
		using (var taskQueue = new TaskQueue()) {
			var queuedTask = taskQueue.Enqueue(() => {
				throw new TaskCanceledException();
			});

			Exception? exception = null;
			try {
				await queuedTask;
			} catch (Exception ex) {
				exception = ex;
			}

			exception.ShouldBeOfType<TaskCanceledException>();
		}
	}
}