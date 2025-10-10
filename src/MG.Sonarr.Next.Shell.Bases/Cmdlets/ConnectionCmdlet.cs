using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Shell.Context;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets
{
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
        protected void DisconnectContext()
        {
            this.UnsetContext();
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
}

