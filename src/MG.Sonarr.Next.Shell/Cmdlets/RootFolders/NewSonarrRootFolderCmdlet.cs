using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Shell.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Cmdlets.RootFolders
{
    [Cmdlet(VerbsCommon.New, "SonarrRootFolder", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    public sealed class NewSonarrRootFolderCmdlet : SonarrApiCmdletBase
    {
        MetadataTag Tag { get; set; } = null!;

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0)]
        [Alias("FullName")]
        public string Path { get; set; } = string.Empty;

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            this.Tag = provider.GetRequiredService<IMetadataResolver>()[Meta.ROOT_FOLDER];
        }
        protected override void Process(IServiceProvider provider)
        {
            string path = this.GetAbsolutePath(this.Path);
            if (this.ShouldProcess(path, "Register Root Folder"))
            {
                this.RegisterFolder(path, s => new { Path = s });
            }
        }

        /// <exception cref="SonarrParameterException"/>
        private string GetAbsolutePath(string providedPath)
        {
            string path = this.GetUnresolvedProviderPathFromPSPath(providedPath);
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new SonarrParameterException(nameof(this.Path), ParameterErrorType.Missing, null,
                    new DirectoryNotFoundException($"Unable to resolve the path -> {providedPath}"));
            }

            return path;
        }
        private void RegisterFolder<T>(string path, Func<string, T> toBody) where T : notnull
        {
            T obj = toBody(path);
            var response = this.SendPostRequest<T, PSObject>(this.Tag.UrlBase, obj);
            if (response.TryPickT0(out var pso, out var error))
            {
                this.WriteObject(pso);
            }
            else
            {
                this.WriteError(error);
            }
        }
    }
}
