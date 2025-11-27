namespace SqlStreamStore.TestUtils.Postgres;

using System;
using System.Threading;
using System.Threading.Tasks;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Common;
using Ductus.FluentDocker.Services;
using Npgsql;
using Polly;

public class PostgresContainer : PostgresDatabaseManager {
	private readonly IContainerService _containerService;
	private const string Image = "postgres:10.4-alpine";
	private const string ContainerName = "sql-stream-store-tests-postgres";
	private const int Port = 5432;

	public override string ConnectionString => ConnectionStringBuilder.ConnectionString;

	public PostgresContainer(string databaseName)
		: base(databaseName) {
		_containerService = new Builder()
			.UseHost()
			.FromUri(new DockerUri(Environment.GetEnvironmentVariable("DOCKER_HOST") ?? "unix:///run/user/1000/docker.sock"))
			.UseContainer()
			.WithName(ContainerName)
			.UseImage(Image)
			.KeepRunning()
			.ReuseIfExists()
			.ExposePort(Port, Port)
			.Command("-N", "500")
			.Build();
	}

	public async Task Start(CancellationToken cancellationToken = default) {
		_containerService.Start();

		await Policy
			.Handle<NpgsqlException>()
			.WaitAndRetryAsync(30, _ => TimeSpan.FromMilliseconds(500))
			.ExecuteAsync(async () => {
				using (var connection = new NpgsqlConnection(DefaultConnectionString)) {
					await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
				}
			});
	}

	private NpgsqlConnectionStringBuilder ConnectionStringBuilder => new() {
		Database = DatabaseName,
		Password = OperatingSystem.IsWindows()
			? "password"
			: null,
		Port = Port,
		Username = "postgres",
		Host = "localhost",
		Pooling = true,
		MaxPoolSize = 1024
	};
}
