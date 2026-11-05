using MG.Sonarr.Next.Exceptions.Shell;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Json.Naming;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Services.Jobs;
using MG.Sonarr.Next.Services.Testing;
using MG.Sonarr.Next.Services.Time;
using MG.Sonarr.Next.Shell.Attributes;
using System.Reflection;
using System.Text.Json;

namespace MG.Sonarr.Next.Shell.Context;

#if DEBUG
public static class CmdletContextExtensions
#else
    internal static class CmdletContextExtensions
#endif
{
	/// <exception cref="ContextNotSetException">There's no connection.</exception>
	internal static IServiceScope CreateScope<T>(this T cmdlet) where T : PSCmdlet, IIsRunning<T>, IScopeCmdlet<T>
	{
		//if (!T.IsRunningCommand(cmdlet))
		//{
		//    throw new InvalidOperationException("Don't execute me weird.");
		//}

		return SonarrContext.CreateScope().WithCmdlet(cmdlet);
	}

	/// <exception cref="ContextNotSetException">There's no connection.</exception>
	internal static IServiceScope CreateScope(this ScopedTransformationAttribute attribute)
	{
		return SonarrContext.CreateScope();
	}

#if DEBUG
	[Obsolete("Only used in interactive PowerShell testing. Never should be called in the code directly.", error: true)]
	public static IMetadataResolver GetResolver()
	{
		return SonarrContext.GetProvider().GetRequiredService<IMetadataResolver>();
	}

	[Obsolete("Only used in interactive PowerShell testing. Never should be called in the code directly.", error: true)]
	public static JsonSerializerOptions GetSerializerOptions()
	{
		return SonarrContext.GetProvider().GetRequiredService<ISonarrJsonOptions>().ForSerializing;
	}
#endif

	internal static IServiceScope SetContext<T>(this T cmdlet, Assembly cmdletAssembly, Action<IServiceCollection> addAdditionalServices)
		where T : PSCmdlet, IConnectContextCmdlet, IIsRunning<T>
	{
		return SonarrContext.Initialize(cmdlet.GetConnectionSettings(), cmdletAssembly, cmdlet.MyInvocation.BoundParameters, addAdditionalServices)
			.WithCmdlet(cmdlet);
	}
	internal static void UnsetContext<T>(this T _) where T : IDisconnectContextCmdlet, IScopeCmdlet<T>
	{
		SonarrContext.Deinitialize();
	}
	/// <summary>
	/// Removes the connection that the specified scope belongs to, when it's still the module's connection.
	/// </summary>
	/// <remarks>
	/// A connection that another runspace made since the scope's was made stays the module's connection.
	/// </remarks>
	/// <param name="_">The cmdlet that made the connection.</param>
	/// <param name="connectScope">The scope that <see cref="SetContext{T}(T, Assembly, Action{IServiceCollection})"/>
	/// returned for the connection.</param>
	internal static void UnsetContext<T>(this T _, IServiceScope connectScope) where T : IDisconnectContextCmdlet, IScopeCmdlet<T>
	{
		SonarrContext.Deinitialize(connectScope);
	}
	internal static ServiceProvider BuildTestContext<T>(this T _, IConnectionSettings settings, Assembly cmdletAssembly, Action<IServiceCollection> addAdditionalServices)
		where T : PSCmdlet, ITestContextCmdlet
	{
		return SonarrContext.Build(settings, cmdletAssembly, addAdditionalServices);
	}

	/// <summary>
	/// Records the cmdlet that the specified scope belongs to, when the cmdlet is an <see cref="IApiCmdlet"/>.
	/// </summary>
	/// <remarks>
	/// The clients that the scope resolves put the cmdlet in each request that they send, where the HTTP handlers find it to
	/// write its verbose and debug output. Any other cmdlet leaves the scope without one.
	/// </remarks>
	/// <param name="scope">The scope that was created for <paramref name="cmdlet"/>.</param>
	/// <param name="cmdlet">The cmdlet that runs in <paramref name="scope"/>.</param>
	/// <returns><paramref name="scope"/>, for chaining.</returns>
	private static IServiceScope WithCmdlet(this IServiceScope scope, PSCmdlet cmdlet)
	{
		if (cmdlet is IApiCmdlet apiCmdlet)
		{
			scope.ServiceProvider.GetRequiredService<ApiCmdletAccessor>().Cmdlet = apiCmdlet;
		}

		return scope;
	}
}

/// <summary>
/// Holds the module's connection, which every runspace in the process shares.
/// </summary>
/// <remarks>
/// A connection is replaced only once its successor is built, so a cmdlet that starts while another runspace connects
/// gets either connection. Each scope holds a lease on the connection it came from, and a connection that has been
/// replaced or removed disposes its provider once its last scope ends, so a cmdlet that is running when another
/// runspace connects or disconnects finishes on the connection it started with.
/// </remarks>
file static class SonarrContext
{
	static Connection? _connection;

	/// <summary>
	/// Gets the provider of the module's connection, without a lease on it.
	/// </summary>
	/// <exception cref="ContextNotSetException">There's no connection.</exception>
	internal static IServiceProvider GetProvider()
	{
		return Volatile.Read(ref _connection)?.Provider ?? ThrowNotSet<IServiceProvider>();
	}

	/// <summary>
	/// Creates a scope on the module's connection, which keeps the connection's provider alive until the scope is
	/// disposed.
	/// </summary>
	/// <exception cref="ContextNotSetException">There's no connection.</exception>
	internal static IServiceScope CreateScope()
	{
		// A connection that can't take a lease has already left the field, so the next read sees its successor or none.
		while (Volatile.Read(ref _connection) is { } connection)
		{
			if (connection.TryCreateScope(out IServiceScope? scope))
			{
				return scope;
			}
		}

		return ThrowNotSet<IServiceScope>();
	}

	/// <exception cref="ContextNotSetException"></exception>
	[DoesNotReturn]
	private static T ThrowNotSet<T>()
	{
		throw new ContextNotSetException(new CmdletScopeNotReadyException());
	}

	// Disconnecting without a connection does nothing.
	internal static void Deinitialize()
	{
		Interlocked.Exchange(ref _connection, null)?.Release();
	}

	// Removes the connection that the scope came from, unless another connection has replaced it already.
	internal static void Deinitialize(IServiceScope connectScope)
	{
		if (connectScope is ConnectionScope { Connection: { } connection }
			&& ReferenceEquals(Interlocked.CompareExchange(ref _connection, null, connection), connection))
		{
			connection.Release();
		}
	}

	internal static IServiceScope Initialize(IConnectionSettings settings, Assembly cmdletAssembly, Dictionary<string, object?> boundParameters, Action<IServiceCollection> configureServices)
	{
		// A new connection replaces the current one, so its test and the cmdlets after it use the settings it was given,
		// and a connection that fails to build leaves none.
		ServiceProvider provider;
		try
		{
			provider = Build(settings, cmdletAssembly, configureServices);
		}
		catch
		{
			Deinitialize();
			throw;
		}

		Connection connection = new(provider);
		if (!connection.TryCreateScope(out IServiceScope? scope))
		{
			Debug.Fail("A new connection always takes a lease.");
			throw new InvalidOperationException("The new connection was disposed before it was used.");
		}

		Interlocked.Exchange(ref _connection, connection)?.Release();
		return scope;
	}

	// Builds the services of a connection with the specified settings. Initialize makes them the module's connection,
	// and a test of a connection uses them on their own.
	internal static ServiceProvider Build(IConnectionSettings settings, Assembly cmdletAssembly, Action<IServiceCollection> configureServices)
	{
		ServiceCollection services = new();
		configureServices(services);

		services
			//.AddClock(mock => mock.GetNow = c => c.Now.AddDays(-7d))
			.AddClock()
			.AddSonarrClient(cmdletAssembly, settings, (provider, options) =>
			{
				options.PropertyNamingPolicy = JsonSpanCamelCaseNamingPolicy.SpanPolicy;
				options.PropertyNameCaseInsensitive = true;
				options.WriteIndented = true;
			})
			.AddCommandTracker()
			.AddTestingService();

		ServiceProviderOptions providerOptions = new()
		{
#if !RELEASE
			ValidateOnBuild = true,
			ValidateScopes = true,
#else
                ValidateOnBuild = false,
                ValidateScopes = false,
#endif
		};

		return services.BuildServiceProvider(providerOptions);
	}
}

/// <summary>
/// A connection's provider, with a count of the references that keep it alive: one for the module while the
/// connection is the module's, and one for each scope created on it.
/// </summary>
file sealed class Connection
{
	private readonly ServiceProvider _provider;
	private int _references = 1;

	internal ServiceProvider Provider => _provider;

	internal Connection(ServiceProvider provider)
	{
		_provider = provider;
	}

	/// <summary>
	/// Creates a scope on the connection's provider, with a lease that the scope releases when it's disposed.
	/// </summary>
	/// <param name="scope">When this method returns <see langword="true"/>, the new scope.</param>
	/// <returns>
	/// <see langword="true"/> if the connection is still alive; otherwise, <see langword="false"/>, when its last
	/// reference has been released and its provider disposed.
	/// </returns>
	internal bool TryCreateScope([NotNullWhen(true)] out IServiceScope? scope)
	{
		int count = Volatile.Read(ref _references);
		while (count > 0)
		{
			int seen = Interlocked.CompareExchange(ref _references, count + 1, count);
			if (seen == count)
			{
				try
				{
					scope = new ConnectionScope(this, _provider.CreateScope());
					return true;
				}
				catch
				{
					this.Release();
					throw;
				}
			}

			count = seen;
		}

		scope = null;
		return false;
	}

	/// <summary>
	/// Releases one reference, and disposes the provider when it was the last one.
	/// </summary>
	internal void Release()
	{
		if (Interlocked.Decrement(ref _references) == 0)
		{
			_provider.Dispose();
		}
	}
}

/// <summary>
/// A scope that holds a lease on the connection it was created on, and releases it once the scope is disposed.
/// </summary>
file sealed class ConnectionScope : IServiceScope
{
	private readonly IServiceScope _scope;
	private Connection? _connection;

	/// <summary>
	/// Gets the connection that the scope was created on, or <see langword="null"/> once the scope is disposed.
	/// </summary>
	internal Connection? Connection => Volatile.Read(ref _connection);

	public IServiceProvider ServiceProvider => _scope.ServiceProvider;

	internal ConnectionScope(Connection connection, IServiceScope scope)
	{
		_connection = connection;
		_scope = scope;
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _connection, null) is { } connection)
		{
			try
			{
				_scope.Dispose();
			}
			finally
			{
				connection.Release();
			}
		}
	}
}
