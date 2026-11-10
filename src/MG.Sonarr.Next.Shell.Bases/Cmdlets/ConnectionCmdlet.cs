using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Shell.Context;

namespace MG.Sonarr.Next.Shell.Cmdlets;

/// <summary>
/// Provides the base for a cmdlet that makes the module's connection to a Sonarr instance, which every runspace in
/// the process shares.
/// </summary>
/// <remarks>
/// <para>
/// A derived cmdlet supplies the connection's settings through <see cref="GetConnectionSettings"/>, makes the
/// connection with <see cref="ConnectContext(Action{IServiceCollection})"/>, checks it, and removes it again with
/// <see cref="DisconnectContext(IServiceScope)"/> when the check fails.
/// </para>
/// <para>
/// The new connection replaces the earlier one once it's built. A cmdlet that is running on the earlier connection
/// finishes on it, and the earlier connection's services are disposed once its last cmdlet ends.
/// </para>
/// </remarks>
public abstract class ConnectCmdlet : PSCmdlet,
	IConnectContextCmdlet,
	IDisconnectContextCmdlet,
	IIsRunning<ConnectCmdlet>,
	IScopeCmdlet<ConnectCmdlet>
{
	/// <summary>
	/// When implemented in a derived class, gets the settings that the connection is built with.
	/// </summary>
	/// <remarks>
	/// <see cref="ConnectContext(Action{IServiceCollection})"/> calls it once for each connection that it makes.
	/// </remarks>
	/// <returns>The connection's settings, such as the instance's URL and API key.</returns>
	protected abstract IConnectionSettings GetConnectionSettings();
	/// <summary>
	/// Gets the settings that the connection is built with, from <see cref="GetConnectionSettings"/>.
	/// </summary>
	/// <returns>The connection's settings.</returns>
	IConnectionSettings IConnectContextCmdlet.GetConnectionSettings() => this.GetConnectionSettings();

	/// <summary>
	/// Builds a connection with the settings from <see cref="GetConnectionSettings"/> and the specified additional
	/// services, makes it the module's connection, and creates a scope on it.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The connection replaces the module's earlier connection, if any, once it's built, and when it fails to build,
	/// the earlier connection is removed too, so the module has no connection. The services are the module's own and
	/// those that <paramref name="addAdditionalServices"/> registers, and the metadata tags list the cmdlets in the
	/// derived cmdlet's assembly that each tag's objects can be piped to.
	/// </para>
	/// <para>
	/// When the cmdlet implements <see cref="MG.Sonarr.Next.Services.Http.IApiCmdlet"/>, the clients that the scope
	/// resolves write the cmdlet's verbose and debug output for their requests. The scope keeps the connection's
	/// services alive until it's disposed, which the caller does once it has checked the connection.
	/// </para>
	/// </remarks>
	/// <param name="addAdditionalServices">A callback that registers the services that the cmdlets add.</param>
	/// <returns>A scope on the new connection, which the caller disposes.</returns>
	protected IServiceScope ConnectContext(Action<IServiceCollection> addAdditionalServices)
	{
		IServiceScope scope = this.SetContext(this.GetType().Assembly, addAdditionalServices);

		return scope;
	}
	/// <summary>
	/// Removes the connection that <see cref="ConnectContext(Action{IServiceCollection})"/> made, unless another
	/// connection has replaced it since.
	/// </summary>
	/// <remarks>
	/// A connection that another runspace made after this one stays the module's connection, so a failed check leaves
	/// another runspace's connection in place. Once <paramref name="connectScope"/> is disposed, the method does
	/// nothing, so the caller calls it first.
	/// </remarks>
	/// <param name="connectScope">
	/// The scope that <see cref="ConnectContext(Action{IServiceCollection})"/> returned for the connection.
	/// </param>
	protected void DisconnectContext(IServiceScope connectScope)
	{
		this.UnsetContext(connectScope);
	}

	private static bool _checked;
	/// <summary>
	/// Gets the flag that <see cref="IScopeCmdlet{T}.SetChecked(bool)"/> last set for <see cref="ConnectCmdlet"/>.
	/// </summary>
	/// <remarks>
	/// <para>The flag is shared by every <see cref="ConnectCmdlet"/> in the process.</para>
	/// <para>TODO: Nothing in the solution calls this member. Clarify what the flag records.</para>
	/// </remarks>
	/// <returns>The flag's value.</returns>
	static bool IScopeCmdlet<ConnectCmdlet>.HasChecked()
	{
		return _checked;
	}

	/// <summary>
	/// Sets the flag that <see cref="IScopeCmdlet{T}.HasChecked"/> returns for <see cref="ConnectCmdlet"/>.
	/// </summary>
	/// <remarks>
	/// <para>The flag is shared by every <see cref="ConnectCmdlet"/> in the process.</para>
	/// <para>TODO: Nothing in the solution calls this member. Clarify what the flag records.</para>
	/// </remarks>
	/// <param name="toggle">The flag's new value.</param>
	static void IScopeCmdlet<ConnectCmdlet>.SetChecked(bool toggle)
	{
		_checked = toggle;
	}
}

/// <summary>
/// Provides the base for a cmdlet that removes the module's connection to a Sonarr instance.
/// </summary>
/// <remarks>
/// Removing the connection affects every runspace in the process. A cmdlet that is running on the connection finishes
/// on it, and the connection's services are disposed once its last cmdlet ends.
/// </remarks>
public abstract class DisconnectCmdlet : PSCmdlet,
	IDisconnectContextCmdlet,
	IIsRunning<DisconnectCmdlet>,
	IScopeCmdlet<DisconnectCmdlet>
{
	/// <summary>
	/// Removes the module's connection, so that cmdlets that start afterward fail until a new connection is made.
	/// </summary>
	/// <remarks>
	/// When there's no connection, the method does nothing.
	/// </remarks>
	protected void DisconnectContext()
	{
		this.UnsetContext();
	}
	/// <summary>
	/// Returns whether <see cref="DisconnectCmdlet"/> has been checked, which is always the case.
	/// </summary>
	/// <remarks>
	/// <para>TODO: Nothing in the solution calls this member. Clarify what the check is.</para>
	/// </remarks>
	/// <returns>Always <see langword="true"/>.</returns>
	static bool IScopeCmdlet<DisconnectCmdlet>.HasChecked()
	{
		return true;
	}
	/// <summary>
	/// Determines whether the specified cmdlet is running as a command, which this class always reports that it isn't.
	/// </summary>
	/// <remarks>
	/// <para>
	/// TODO: Nothing in the solution calls this member, and it returns <see langword="false"/> where the interface's
	/// default returns <see langword="true"/>. Clarify what it's meant to report.
	/// </para>
	/// </remarks>
	/// <param name="cmdlet">The cmdlet to check.</param>
	/// <returns>Always <see langword="false"/>.</returns>
	static bool IIsRunning<DisconnectCmdlet>.IsRunningCommand(DisconnectCmdlet cmdlet)
	{
		return false;
	}
	/// <summary>
	/// Ignores the specified flag, since <see cref="IScopeCmdlet{T}.HasChecked"/> always returns <see langword="true"/>
	/// for <see cref="DisconnectCmdlet"/>.
	/// </summary>
	/// <remarks>
	/// <para>TODO: Nothing in the solution calls this member. Clarify what the check is.</para>
	/// </remarks>
	/// <param name="toggle">The flag's new value, which isn't stored.</param>
	static void IScopeCmdlet<DisconnectCmdlet>.SetChecked(bool toggle)
	{
		return;
	}
}

/// <summary>
/// Provides the base for a cmdlet that checks a Sonarr instance's URL and API key without changing the module's
/// connection.
/// </summary>
public abstract class TestConnectionCmdlet : PSCmdlet,
	ITestContextCmdlet
{
	/// <summary>
	/// Builds a service provider with the services that a connection with the specified settings would have.
	/// </summary>
	/// <remarks>
	/// The provider isn't the module's connection, so a test leaves the connection as it was. The services are the
	/// module's own and those that <paramref name="addAdditionalServices"/> registers, and the metadata tags list the
	/// cmdlets in the derived cmdlet's assembly that each tag's objects can be piped to. A scope that the caller creates
	/// on the provider holds no cmdlet, so its requests write no verbose or debug output.
	/// </remarks>
	/// <param name="settings">The settings to build the services with, such as the instance's URL and API key.</param>
	/// <param name="addAdditionalServices">A callback that registers the services that the cmdlets add.</param>
	/// <returns>A new service provider, which the caller disposes.</returns>
	protected ServiceProvider CreateTestContext(IConnectionSettings settings, Action<IServiceCollection> addAdditionalServices)
	{
		return this.BuildTestContext(settings, this.GetType().Assembly, addAdditionalServices);
	}
}

