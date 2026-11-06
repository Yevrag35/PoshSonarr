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
/// <summary>
/// Provides the methods that connect the module to a Sonarr instance, disconnect it, and create the service scopes that
/// cmdlets and argument transformations run in.
/// </summary>
/// <remarks>
/// <para>
/// The class is public only in Debug builds, so that an interactive PowerShell session can call its debugging members
/// on the current connection. In every other build, it's internal and has no debugging members.
/// </para>
/// <para>
/// Every runspace in the process shares the one connection that these methods manage, and they're safe to call from
/// several runspaces at once.
/// </para>
/// </remarks>
public static class CmdletContextExtensions
#else
/// <summary>
/// Provides the methods that connect the module to a Sonarr instance, disconnect it, and create the service scopes that
/// cmdlets and argument transformations run in.
/// </summary>
/// <remarks>
/// <para>
/// The class is public only in Debug builds, so that an interactive PowerShell session can call its debugging members
/// on the current connection. In every other build, it's internal and has no debugging members.
/// </para>
/// <para>
/// Every runspace in the process shares the one connection that these methods manage, and they're safe to call from
/// several runspaces at once.
/// </para>
/// </remarks>
    internal static class CmdletContextExtensions
#endif
{
	/// <summary>
	/// Creates the service scope that the specified cmdlet runs in, on the module's connection.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The scope holds a lease on the connection's provider, so the provider stays alive until the scope is disposed, even
	/// when another runspace connects or disconnects in the meantime. When <paramref name="cmdlet"/> is an
	/// <see cref="IApiCmdlet"/>, the scope's <see cref="ApiCmdletAccessor"/> holds it, so the HTTP handlers can write
	/// its verbose and debug output.
	/// </para>
	/// <para>
	/// The constraints on <typeparamref name="T"/> limit the method to the module's cmdlet base classes. Nothing in the
	/// method calls the members of <see cref="IIsRunning{T}"/> or <see cref="IScopeCmdlet{T}"/>.
	/// </para>
	/// </remarks>
	/// <typeparam name="T">The type of the cmdlet.</typeparam>
	/// <param name="cmdlet">The cmdlet that runs in the new scope.</param>
	/// <returns>A new scope on the module's connection, which the caller disposes when the cmdlet ends.</returns>
	/// <exception cref="ContextNotSetException">Thrown when the module has no connection.</exception>
	internal static IServiceScope CreateScope<T>(this T cmdlet) where T : PSCmdlet, IIsRunning<T>, IScopeCmdlet<T>
	{
		//if (!T.IsRunningCommand(cmdlet))
		//{
		//    throw new InvalidOperationException("Don't execute me weird.");
		//}

		return SonarrContext.CreateScope().WithCmdlet(cmdlet);
	}

	/// <summary>
	/// Creates the service scope that the specified argument transformation runs in, on the module's connection.
	/// </summary>
	/// <remarks>
	/// The scope holds a lease on the connection's provider, so the provider stays alive until the scope is disposed. It
	/// holds no cmdlet, so a request sent from it writes no verbose or debug output.
	/// </remarks>
	/// <param name="attribute">The transformation attribute that runs in the new scope. The method doesn't read it.</param>
	/// <returns>A new scope on the module's connection, which the caller disposes when the transformation ends.</returns>
	/// <exception cref="ContextNotSetException">Thrown when the module has no connection.</exception>
	internal static IServiceScope CreateScope(this ScopedTransformationAttribute attribute)
	{
		return SonarrContext.CreateScope();
	}

#if DEBUG
	/// <summary>
	/// Gets the metadata resolver of the module's current connection, for interactive debugging.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The method exists only in Debug builds, for a PowerShell session to call as
	/// <c>[MG.Sonarr.Next.Shell.Context.CmdletContextExtensions]::GetResolver()</c>. It's marked obsolete as an error so
	/// that C# code can't call it.
	/// </para>
	/// <para>
	/// It holds no lease on the connection, so once another runspace connects or disconnects, the resolver can belong to a
	/// connection whose services have been disposed.
	/// </para>
	/// </remarks>
	/// <returns>The <see cref="IMetadataResolver"/> of the current connection.</returns>
	/// <exception cref="ContextNotSetException">Thrown when the module has no connection.</exception>
	/// <exception cref="ObjectDisposedException">Thrown when another runspace replaced or removed the connection, and its
	/// services were disposed, while the method ran.</exception>
	[Obsolete("Only used in interactive PowerShell testing. Never should be called in the code directly.", error: true)]
	public static IMetadataResolver GetResolver()
	{
		return SonarrContext.GetProvider().GetRequiredService<IMetadataResolver>();
	}

	/// <summary>
	/// Gets the JSON options that the module's current connection serializes request bodies with, for interactive
	/// debugging.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The method exists only in Debug builds, for a PowerShell session to call as
	/// <c>[MG.Sonarr.Next.Shell.Context.CmdletContextExtensions]::GetSerializerOptions()</c>. It's marked obsolete as an
	/// error so that C# code can't call it.
	/// </para>
	/// <para>
	/// It holds no lease on the connection, so once another runspace connects or disconnects, the options can belong to a
	/// connection whose services have been disposed. The connection builds its options the first time they're resolved,
	/// so the first call can build them.
	/// </para>
	/// </remarks>
	/// <returns>The <see cref="JsonSerializerOptions"/> that the current connection serializes with.</returns>
	/// <exception cref="ContextNotSetException">Thrown when the module has no connection.</exception>
	/// <exception cref="ObjectDisposedException">Thrown when another runspace replaced or removed the connection, and its
	/// services were disposed, while the method ran.</exception>
	[Obsolete("Only used in interactive PowerShell testing. Never should be called in the code directly.", error: true)]
	public static JsonSerializerOptions GetSerializerOptions()
	{
		return SonarrContext.GetProvider().GetRequiredService<ISonarrJsonOptions>().ForSerializing;
	}
#endif

	/// <summary>
	/// Builds a connection from the specified cmdlet's settings, makes it the module's connection, and creates the scope
	/// that the cmdlet goes on to run in.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The new connection replaces the module's current one only once it's built, so a cmdlet that starts in another
	/// runspace meanwhile gets one connection or the other. The connection it replaces is disposed once the last scope
	/// on it ends. When the build fails, the method removes the current connection too, so the module is left without
	/// one.
	/// </para>
	/// <para>
	/// The returned scope holds a lease on the new connection, and its <see cref="ApiCmdletAccessor"/> holds
	/// <paramref name="cmdlet"/> when it's an <see cref="IApiCmdlet"/>. Pass the scope to
	/// <see cref="UnsetContext{T}(T, IServiceScope)"/> to remove this connection when its check of the URL and key
	/// fails.
	/// </para>
	/// </remarks>
	/// <typeparam name="T">The type of the connecting cmdlet.</typeparam>
	/// <param name="cmdlet">The connecting cmdlet, which supplies the connection's settings.</param>
	/// <param name="cmdletAssembly">The assembly whose cmdlets the metadata tags list as able to take piped
	/// objects.</param>
	/// <param name="addAdditionalServices">The callback that adds the Shell project's own services to the connection's
	/// service collection, before the module's core services are added.</param>
	/// <returns>A new scope on the new connection, which the caller disposes.</returns>
	/// <exception cref="AggregateException">Thrown, in builds other than Release, when the container's validation finds a
	/// service that it can't build.</exception>
	internal static IServiceScope SetContext<T>(this T cmdlet, Assembly cmdletAssembly, Action<IServiceCollection> addAdditionalServices)
		where T : PSCmdlet, IConnectContextCmdlet, IIsRunning<T>
	{
		return SonarrContext.Initialize(cmdlet.GetConnectionSettings(), cmdletAssembly, cmdlet.MyInvocation.BoundParameters, addAdditionalServices)
			.WithCmdlet(cmdlet);
	}
	/// <summary>
	/// Removes the module's connection, whichever connection it is.
	/// </summary>
	/// <remarks>
	/// The connection's provider is disposed once the last scope on it ends, so a cmdlet that's running in another
	/// runspace finishes on it. Without a connection, the method does nothing.
	/// </remarks>
	/// <typeparam name="T">The type of the disconnecting cmdlet.</typeparam>
	/// <param name="_">The disconnecting cmdlet. The method doesn't read it.</param>
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
	/// <typeparam name="T">The type of the cmdlet that made the connection.</typeparam>
	/// <param name="_">The cmdlet that made the connection. The method doesn't read it.</param>
	/// <param name="connectScope">The scope that <see cref="SetContext{T}(T, Assembly, Action{IServiceCollection})"/>
	/// returned for the connection. A scope that's already disposed, or that came from anywhere else, removes
	/// nothing.</param>
	internal static void UnsetContext<T>(this T _, IServiceScope connectScope) where T : IDisconnectContextCmdlet, IScopeCmdlet<T>
	{
		SonarrContext.Deinitialize(connectScope);
	}
	/// <summary>
	/// Builds the services that a connection with the specified settings would have, without making them the module's
	/// connection.
	/// </summary>
	/// <remarks>
	/// The module's connection stays as it was, so a test of a URL and key leaves every runspace's cmdlets on the
	/// connection they had.
	/// </remarks>
	/// <typeparam name="T">The type of the testing cmdlet.</typeparam>
	/// <param name="_">The testing cmdlet. The method doesn't read it.</param>
	/// <param name="settings">The settings of the connection to test.</param>
	/// <param name="cmdletAssembly">The assembly whose cmdlets the metadata tags list as able to take piped
	/// objects.</param>
	/// <param name="addAdditionalServices">The callback that adds the Shell project's own services to the service
	/// collection, before the module's core services are added.</param>
	/// <returns>A new provider, which the caller owns and disposes when the test ends.</returns>
	/// <exception cref="AggregateException">Thrown, in builds other than Release, when the container's validation finds a
	/// service that it can't build.</exception>
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
/// Provides the module's connection, which every runspace in the process shares.
/// </summary>
/// <remarks>
/// <para>
/// A connection is replaced only once its successor is built, so a cmdlet that starts while another runspace connects
/// gets either connection. Each scope holds a lease on the connection it came from, and a connection that has been
/// replaced or removed disposes its provider once its last scope ends, so a cmdlet that is running when another
/// runspace connects or disconnects finishes on the connection it started with.
/// </para>
/// <para>
/// Every member is safe to call from several threads at once. The field changes only through
/// <see cref="Interlocked"/> operations and is read with <see cref="Volatile"/>, and a connection that leaves the field
/// loses the module's reference exactly once, through <see cref="Connection.Release"/>. When two connections are made
/// at once, the one that's published last becomes the module's connection, and the other is disposed once its scopes
/// end. What isn't safe is using the provider from <see cref="GetProvider"/> while another runspace connects or
/// disconnects, since that provider holds no lease and can be disposed under its caller.
/// </para>
/// </remarks>
file static class SonarrContext
{
	/// <summary>
	/// The module's connection, or <see langword="null"/> when there's none.
	/// </summary>
	private static Connection? _connection;

	/// <summary>
	/// Gets the provider of the module's connection, without a lease on it.
	/// </summary>
	/// <remarks>
	/// The provider can be disposed as soon as another runspace replaces or removes the connection, so the method is for
	/// debugging only. Code that resolves services creates a scope with <see cref="CreateScope"/> instead.
	/// </remarks>
	/// <returns>The <see cref="IServiceProvider"/> of the module's connection.</returns>
	/// <exception cref="ContextNotSetException">Thrown when the module has no connection.</exception>
	internal static IServiceProvider GetProvider()
	{
		return Volatile.Read(ref _connection)?.Provider ?? ThrowNotSet<IServiceProvider>();
	}

	/// <summary>
	/// Creates a scope on the module's connection, which keeps the connection's provider alive until the scope is
	/// disposed.
	/// </summary>
	/// <remarks>
	/// A connection whose last reference has been released can't take a lease, and by then it has already left the
	/// field, so the method reads the field again and tries the connection that replaced it, if any.
	/// </remarks>
	/// <returns>A new scope that holds a lease on the module's connection, which the caller disposes.</returns>
	/// <exception cref="ContextNotSetException">Thrown when the module has no connection.</exception>
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

	/// <summary>
	/// Throws the exception that reports that the module has no connection.
	/// </summary>
	/// <typeparam name="T">The return type that the caller needs. The method never returns.</typeparam>
	/// <returns>Nothing, since the method always throws.</returns>
	/// <exception cref="ContextNotSetException">Thrown always, with a <see cref="CmdletScopeNotReadyException"/> as its
	/// inner exception.</exception>
	[DoesNotReturn]
	private static T ThrowNotSet<T>()
	{
		throw new ContextNotSetException(new CmdletScopeNotReadyException());
	}

	/// <summary>
	/// Removes the module's connection, whichever connection it is, and releases the module's reference to it.
	/// </summary>
	/// <remarks>
	/// The connection's provider is disposed once the last scope on it ends. Without a connection, the method does
	/// nothing.
	/// </remarks>
	internal static void Deinitialize()
	{
		Interlocked.Exchange(ref _connection, null)?.Release();
	}

	/// <summary>
	/// Removes the connection that the specified scope came from, when it's still the module's connection, and releases
	/// the module's reference to it.
	/// </summary>
	/// <remarks>
	/// A connection that another runspace made since stays the module's connection. A scope that isn't a
	/// <see cref="ConnectionScope"/>, or that has been disposed, removes nothing.
	/// </remarks>
	/// <param name="connectScope">The scope that <see cref="Initialize"/> returned for the connection.</param>
	internal static void Deinitialize(IServiceScope connectScope)
	{
		if (connectScope is ConnectionScope { Connection: { } connection }
			&& ReferenceEquals(Interlocked.CompareExchange(ref _connection, null, connection), connection))
		{
			connection.Release();
		}
	}

	/// <summary>
	/// Builds a connection with the specified settings, makes it the module's connection, and creates a scope on it.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The scope takes its lease before the connection is published, so it can't lose the connection to a disconnect
	/// in another runspace. The connection that the new one replaces loses the module's reference and is disposed once
	/// its last scope ends.
	/// </para>
	/// <para>
	/// When the build throws, the method removes the current connection before it rethrows, so the module is left
	/// without one rather than on settings that the caller meant to replace.
	/// </para>
	/// </remarks>
	/// <param name="settings">The settings of the new connection.</param>
	/// <param name="cmdletAssembly">The assembly whose cmdlets the metadata tags list as able to take piped
	/// objects.</param>
	/// <param name="boundParameters">The connecting cmdlet's bound parameters. The method doesn't read them.</param>
	/// <param name="configureServices">The callback that adds the Shell project's own services to the service
	/// collection, before the module's core services are added.</param>
	/// <returns>A new scope that holds a lease on the new connection, which the caller disposes.</returns>
	/// <exception cref="AggregateException">Thrown, in builds other than Release, when the container's validation finds a
	/// service that it can't build.</exception>
	/// <exception cref="InvalidOperationException">Thrown when the new connection can't take a lease, which never happens
	/// to a connection that nothing else has seen.</exception>
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

	/// <summary>
	/// Builds the service provider of a connection with the specified settings.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="Initialize"/> makes the provider the module's connection, and a test of a connection uses it on its
	/// own.
	/// </para>
	/// <para>
	/// The provider holds <paramref name="configureServices"/>'s services, the clock, the HTTP client with its JSON
	/// options and metadata tags, the command tracker, and the testing service. The JSON options name properties in
	/// camel case, read names without regard to case, and indent what they write.
	/// </para>
	/// <para>
	/// Builds other than Release turn on the container's <see cref="ServiceProviderOptions.ValidateOnBuild"/> and
	/// <see cref="ServiceProviderOptions.ValidateScopes"/> checks.
	/// </para>
	/// </remarks>
	/// <param name="settings">The settings of the connection.</param>
	/// <param name="cmdletAssembly">The assembly whose cmdlets the metadata tags list as able to take piped
	/// objects.</param>
	/// <param name="configureServices">The callback that adds the Shell project's own services to the service
	/// collection, before the module's core services are added.</param>
	/// <returns>A new provider, which the caller owns.</returns>
	/// <exception cref="AggregateException">Thrown, in builds other than Release, when the container's validation finds a
	/// service that it can't build.</exception>
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
/// Represents a connection's provider, with a count of the references that keep it alive: one for the module while the
/// connection is the module's, and one for each scope created on it.
/// </summary>
/// <remarks>
/// <para>
/// The count starts at 1, the module's reference, and the provider is disposed when it reaches 0. Once it's 0, it never
/// rises again, so a disposed connection can't hand out another lease.
/// </para>
/// <para>
/// <see cref="TryCreateScope(out IServiceScope?)"/> and <see cref="Release"/> are safe to call from several threads at
/// once. Each reference is released exactly once: the module's by whoever removes the connection from the module, and
/// each scope's by that scope's <see cref="ConnectionScope.Dispose"/>.
/// </para>
/// </remarks>
file sealed class Connection
{
	private readonly ServiceProvider _provider;
	private int _references = 1;

	/// <summary>
	/// Gets the connection's provider.
	/// </summary>
	/// <remarks>
	/// Reading it takes no lease, so the provider can already be disposed, or be disposed while the caller uses it.
	/// </remarks>
	/// <value>The <see cref="ServiceProvider"/> that the connection owns.</value>
	internal ServiceProvider Provider => _provider;

	/// <summary>
	/// Initializes a new <see cref="Connection"/> instance with the specified provider and one reference, the module's.
	/// </summary>
	/// <param name="provider">The provider that the connection owns and disposes when its last reference is
	/// released.</param>
	internal Connection(ServiceProvider provider)
	{
		_provider = provider;
	}

	/// <summary>
	/// Creates a scope on the connection's provider, with a lease that the scope releases when it's disposed.
	/// </summary>
	/// <remarks>
	/// The lease is taken with a compare-and-exchange loop that never raises a count of 0, so the method can't revive a
	/// connection whose provider has been disposed. When creating the scope throws, the lease is released before the
	/// exception leaves the method.
	/// </remarks>
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
	/// <remarks>
	/// Each reference is released once. Releasing one more than was taken would drive the count below 0 and leave the
	/// provider as it is.
	/// </remarks>
	internal void Release()
	{
		if (Interlocked.Decrement(ref _references) == 0)
		{
			_provider.Dispose();
		}
	}
}

/// <summary>
/// Represents a scope that holds a lease on the connection it was created on, and releases it once the scope is
/// disposed.
/// </summary>
/// <remarks>
/// <see cref="Dispose"/> is safe to call more than once and from several threads: only the first call disposes the
/// inner scope and releases the lease. Resolving services from <see cref="ServiceProvider"/> during or after disposal
/// isn't safe, as with any <see cref="IServiceScope"/>.
/// </remarks>
file sealed class ConnectionScope : IServiceScope
{
	private readonly IServiceScope _scope;
	private Connection? _connection;

	/// <summary>
	/// Gets the connection that the scope was created on, or <see langword="null"/> once the scope is disposed.
	/// </summary>
	/// <value>The connection that holds the scope's lease, or <see langword="null"/> once the scope is disposed.</value>
	internal Connection? Connection => Volatile.Read(ref _connection);

	/// <inheritdoc/>
	public IServiceProvider ServiceProvider => _scope.ServiceProvider;

	/// <summary>
	/// Initializes a new <see cref="ConnectionScope"/> instance with the specified connection and the scope created on
	/// its provider.
	/// </summary>
	/// <remarks>
	/// The caller has already taken the lease on <paramref name="connection"/> that the new instance releases.
	/// </remarks>
	/// <param name="connection">The connection whose lease the instance holds.</param>
	/// <param name="scope">The scope that was created on the connection's provider, which the instance owns.</param>
	internal ConnectionScope(Connection connection, IServiceScope scope)
	{
		_connection = connection;
		_scope = scope;
	}

	/// <summary>
	/// Disposes the inner scope and releases the scope's lease on its connection.
	/// </summary>
	/// <remarks>
	/// The lease is released even when disposing the inner scope throws, so the connection's provider is still disposed
	/// once its last reference goes. Only the first call does anything.
	/// </remarks>
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
