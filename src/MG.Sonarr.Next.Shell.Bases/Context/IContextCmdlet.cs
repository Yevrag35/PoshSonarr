using MG.Sonarr.Next.Services.Auth;

namespace MG.Sonarr.Next.Shell.Context
{
    internal interface IConnectContextCmdlet
    {
        IConnectionSettings GetConnectionSettings();
    }

    internal interface IDisconnectContextCmdlet
    {
    }
}

