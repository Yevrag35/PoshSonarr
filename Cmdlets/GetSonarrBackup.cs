using MG.Api;
using Sonarr.Api.Endpoints;
using Sonarr.Api.Enums;
using Sonarr.Api.Results;
using System;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SonarrBackup", ConfirmImpact = ConfirmImpact.None)]
    [OutputType(typeof(BackupResult))]
    public class GetSonarrBackup : BaseCmdlet
    {
        [Parameter(Mandatory = false, Position = 0)]
        [Alias("type", "t")]
        public BackupType[] BackupType { get; set; }

        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            var back = new SystemBackup();
            result = Api.Send(back);
            if (BackupType != null)
            {
                FilterOut(BackupType);
            }

            PipeBack<BackupResult>(result);
        }

        private void FilterOut(BackupType[] types)
        {
            for (int i = result.Count - 1; i >= 0; i--)
            {
                bool skip = false;
                var d = result[i];
                var s = (string)d["type"];
                for (int t = 0; t < types.Length; t++)
                {
                    var e = types[t];
                    if (e.ToString().Equals(s, StringComparison.OrdinalIgnoreCase))
                    {
                        skip = true;
                    }
                }
                if (!skip)
                    result.Remove(d);

            }
        }
    }
}
