using MG.Sonarr.Next.Services.Auth;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems.Server
{
    [Cmdlet(VerbsLifecycle.Restart, "SonarrInstance", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [Alias("Restart-Sonarr")]
    public sealed class RestartSonarrInstanceCmdlet : SonarrApiCmdletBase
    {
        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override void Process(IServiceProvider provider)
        {
            if (!this.Force
                &&
                this.ShouldNotProcess(provider))
            {
                return;
            }

            var response = this.SendPostRequest<PSObject>(Constants.RESTART);
            _ = this.TryWriteObject(in response);
        }

        private bool ShouldNotProcess(IServiceProvider provider)
        {
            var settings = provider.GetRequiredService<IConnectionSettings>();
            string host = settings.ServiceUri.GetComponents(UriComponents.Host, UriFormat.Unescaped);

            return !this.ShouldProcess(host, "Restart Sonarr Application");
        }
    }
}
