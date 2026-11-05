using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Shell.Context;

namespace MG.Sonarr.Next.Shell.Cmdlets;

public abstract class ConnectCmdlet : PSCmdlet,
	IConnectContextCmdlet,
	IDisconnectContextCmdlet,
	IIsRunning<ConnectCmdlet>,
	IScopeCmdlet<ConnectCmdlet>
{
	protected abstract IConnectionSettings GetConnectionSettings();
	IConnectionSettings IConnectContextCmdlet.GetConnectionSettings() => this.GetConnectionSettings();

	protected IServiceScope ConnectContext(Action<IServiceCollection> addAdditionalServices)
	{
		IServiceScope scope = this.SetContext(this.GetType().Assembly, addAdditionalServices);

		return scope;
	}
	// Removes the connection that ConnectContext made and returned the scope for, unless another runspace has connected
	// since, so a failed check leaves another runspace's connection in place.
	protected void DisconnectContext(IServiceScope connectScope)
	{
		this.UnsetContext(connectScope);
	}

	static bool _checked;
	static bool IScopeCmdlet<ConnectCmdlet>.HasChecked()
	{
		return _checked;
	}

	static void IScopeCmdlet<ConnectCmdlet>.SetChecked(bool toggle)
	{
		_checked = toggle;
	}
}

public abstract class DisconnectCmdlet : PSCmdlet,
	IDisconnectContextCmdlet,
	IIsRunning<DisconnectCmdlet>,
	IScopeCmdlet<DisconnectCmdlet>
{
	protected void DisconnectContext()
	{
		this.UnsetContext();
	}
	static bool IScopeCmdlet<DisconnectCmdlet>.HasChecked()
	{
		return true;
	}
	static bool IIsRunning<DisconnectCmdlet>.IsRunningCommand(DisconnectCmdlet cmdlet)
	{
		return false;
	}
	static void IScopeCmdlet<DisconnectCmdlet>.SetChecked(bool toggle)
	{
		return;
	}
}

public abstract class TestConnectionCmdlet : PSCmdlet,
	ITestContextCmdlet
{
	// The provider holds the services that a connection with the settings would have, but it isn't the module's
	// connection, so a test leaves the connection as it was. The caller disposes the provider.
	protected ServiceProvider CreateTestContext(IConnectionSettings settings, Action<IServiceCollection> addAdditionalServices)
	{
		return this.BuildTestContext(settings, this.GetType().Assembly, addAdditionalServices);
	}
}

