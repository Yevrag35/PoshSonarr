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

/// <summary>
/// Represents a cmdlet that can remove the module's connection.
/// </summary>
/// <remarks>
/// The interface has no members. It marks the cmdlets that the two <c>UnsetContext</c> overloads of
/// <see cref="CmdletContextExtensions"/> accept: the disconnecting cmdlet, and the connecting cmdlet, which removes the
/// connection it made when its check of the URL and key fails.
/// </remarks>
internal interface IDisconnectContextCmdlet
{
}

/// <summary>
/// Represents a cmdlet that tests connection settings on services of its own, without changing the module's
/// connection.
/// </summary>
/// <remarks>
/// The interface has no members. It marks the cmdlets that
/// <see cref="CmdletContextExtensions.BuildTestContext{T}(T, IConnectionSettings, System.Reflection.Assembly, Action{IServiceCollection})"/>
/// accepts.
/// </remarks>
internal interface ITestContextCmdlet
{
}

