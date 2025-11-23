namespace SqlStreamStore.HAL;

using System.Reflection;
using Microsoft.Extensions.Logging;

public class SqlStreamStoreMiddlewareOptions {
	public bool UseCanonicalUrls { get; set; } = true;
	public Assembly? ServerAssembly { get; set; }

	public ILoggerFactory? LoggerFactory { get; set; }
}
