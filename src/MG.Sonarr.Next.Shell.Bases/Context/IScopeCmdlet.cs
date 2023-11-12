using MG.Sonarr.Next.Shell.Cmdlets;

namespace MG.Sonarr.Next.Shell.Context
{
    internal interface IScopeCmdlet<T>
    {
        static abstract bool HasChecked();
        static abstract void SetChecked(bool toggle);
    }

    internal interface IIsRunning<T> where T : PSCmdlet
    {
        static virtual bool IsRunningCommand(T cmdlet)
        {
            return true;
            //return cmdlet.SessionState is not null && cmdlet.InvokeProvider is not null;
        }
    }
}
