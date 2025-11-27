namespace SqlStreamStore.TestUtils.MsSql;

using System;
using System.Threading;
using System.Threading.Tasks;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Common;
using Ductus.FluentDocker.Model.Common;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Services.Extensions;
using Microsoft.Data.SqlClient;
using Polly;



public class SqlServerContainer : IDisposable {
	//public string DatabaseName { get; }
	private readonly int _hostPort;
	private readonly IContainerService _containerService;
	private const string DefaultContainerNamePrefix = "sql-stream-store-tests-mssql";
	private const string Password = "E@syP@ssw0rd";
	private const string Image = "mcr.microsoft.com/mssql/server:2017-latest";
	public const int DefaultHostPortStart = 11433;
	private const int ContainerPort = 1433;

	public SqlServerContainer(int hostPortStart = DefaultHostPortStart) {
		var dockerHost = new DockerUri(Environment.GetEnvironmentVariable("DOCKER_HOST") ??
		                               "unix:///run/user/1000/docker.sock");

		var hostBuilder = new Builder()
			.UseHost()
			.FromUri(dockerHost);

		(_containerService, _hostPort) = BuildAndStartContainer(DefaultContainerNamePrefix,
			Image,
			hostPortStart,
			ContainerPort,
			("ACCEPT_EULA", "Y"),
			("SA_PASSWORD", Password));

		(IContainerService, int) BuildAndStartContainer(
			string prefix,
			string image,
			int hostPort,
			int containerPort,
			params (string key, object value)[] environment) {
			var containerService = Policy
				.Handle<FluentDockerException>(ex =>
					ex.Message.Contains("Bind for 127.0.0.1:11433 failed: port is already allocated"))
				.Or<FluentDockerException>(ex =>
					ex.Message.Contains(
						"You have to remove (or rename) that container to be able to reuse that name."))
				.Retry(5, (_, _) => hostPort += 1)
				.Execute(() => hostBuilder
					.UseContainer()
					.WithName($"{prefix}-{hostPort}")
					.UseImage(image)
					.DeleteIfExists()
					.ExposePort(hostPort, containerPort)
					.WithEnvironment(Array.ConvertAll(environment, input => $"{input.key}={input.value}"))
					.Build()
					.Start());
			return (containerService, hostPort);
		}
	}

	public SqlConnection CreateConnection()
		=> new SqlConnection(CreateConnectionStringBuilder().ConnectionString);

	public SqlConnectionStringBuilder CreateConnectionStringBuilder()
		=> new($"server=localhost,{_hostPort};User Id=sa;Password={Password};Initial Catalog=master;TrustServerCertificate=True;Max Pool Size=1024");
	public async Task Start(CancellationToken cancellationToken = default) {
		_containerService
			.Start()
			.WaitForMessageInLogs("SQL Server is now ready for client connections", 10000);

		await Policy
			.Handle<SqlException>()
			.WaitAndRetryAsync(30, _ => TimeSpan.FromMilliseconds(500))
			.ExecuteAsync(async () => {
				await using var connection = new SqlConnection(CreateConnectionStringBuilder().ConnectionString);
				await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
			});
	}

	public async Task CreateDatabase(string databaseName, CancellationToken cancellationToken = default) {
		var policy = Policy
			.Handle<SqlException>()
			.WaitAndRetryAsync(5, i => TimeSpan.FromSeconds(i * i));

		await policy.ExecuteAsync(async () => {
			await using var connection = CreateConnection();
			await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

			var createCommand = $@"CREATE DATABASE [{databaseName}]
ALTER DATABASE [{databaseName}] SET SINGLE_USER
ALTER DATABASE [{databaseName}] SET COMPATIBILITY_LEVEL=110
ALTER DATABASE [{databaseName}] SET MULTI_USER";

			await using var command = new SqlCommand(createCommand, connection);
			await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
		});
	}

	public void Dispose() {
		_containerService.Stop();
		_containerService.Remove();
		_containerService.Dispose();
	}
}
