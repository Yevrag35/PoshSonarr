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
using MG.Sonarr.Next.Shell.Exceptions;
using System.Reflection;
using System.Text.Json;

namespace MG.Sonarr.Next.Shell.Context;

#if DEBUG
public static class CmdletContextExtensions
#else
    internal static class CmdletContextExtensions
#endif
{
	internal static IServiceScope CreateScope<T>(this T cmdlet) where T : PSCmdlet, IIsRunning<T>, IScopeCmdlet<T>
	{
		//if (!T.IsRunningCommand(cmdlet))
		//{
		//    throw new InvalidOperationException("Don't execute me weird.");
		//}

		return SonarrContext.GetProvider().CreateScope().WithCmdlet(cmdlet);
	}

	internal static IServiceScope CreateScope(this ScopedTransformationAttribute attribute)
	{
		return SonarrContext.GetProvider().CreateScope();
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

file static class SonarrContext
{
	static ServiceProvider _provider = null!;

	/// <exception cref="ContextNotSetException"/>
	internal static IServiceProvider GetProvider()
	{
		return _provider ?? ThrowNotSet();
	}

	/// <exception cref="ContextNotSetException"></exception>
	[DoesNotReturn]
	private static IServiceProvider ThrowNotSet()
	{
		throw new ContextNotSetException(new CmdletScopeNotReadyException());
	}

	// Disconnecting without a connection does nothing.
	internal static void Deinitialize()
	{
		_provider?.Dispose();
		_provider = null!;
	}

	internal static IServiceScope Initialize(IConnectionSettings settings, Assembly cmdletAssembly, Dictionary<string, object?> boundParameters, Action<IServiceCollection> configureServices)
	{
		// A new connection replaces the current one, so its test and the cmdlets after it use the settings it was given.
		Deinitialize();

		_provider = Build(settings, cmdletAssembly, configureServices);
		return _provider.CreateScope();
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