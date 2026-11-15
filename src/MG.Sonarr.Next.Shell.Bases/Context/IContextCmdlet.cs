using MG.Sonarr.Next.Services.Auth;

namespace MG.Sonarr.Next.Shell.Context;

/// <summary>
/// Provides the connection settings of a cmdlet that connects the module to a Sonarr instance.
/// </summary>
/// <remarks>
/// <see cref="CmdletContextExtensions.SetContext{T}(T, System.Reflection.Assembly, Action{IServiceCollection})"/> takes
/// only a cmdlet that implements it, and builds the module's connection from the settings it returns.
/// </remarks>
internal interface IConnectContextCmdlet
{
	/// <summary>
	/// When implemented in a derived class, gets the settings of the connection that the cmdlet makes.
	/// </summary>
	/// <returns>The settings of the new connection, such as its URL, API key, and timeout.</returns>
	IConnectionSettings GetConnectionSettings();
}

