using Sonarr.Api.Endpoints;
using Sonarr.Api.Enums;
using Sonarr.Api.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SonarrBackup", ConfirmImpact = ConfirmImpact.None)]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(BackupResult))]
    public class GetSonarrBackup : BaseCmdlet
    {
        private List<BackupResult> _list;

        [Parameter(Mandatory = false, Position = 0)]
        [Alias("type", "t")]
        public BackupType[] BackupType { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            _list = new List<BackupResult>();
        }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            var back = new SystemBackup();
            _list.AddRange(Api.SonarrGetAs<BackupResult>(back));
            FilterOut(this.BackupType);
            WriteObject(_list.ToArray(), true);
        }

        private void FilterOut(BackupType[] types)
        {
            var typeInStr = new string[types.Length];
            for (int n = 0; n < types.Length; n++)
            {
                typeInStr[n] = types[n].ToString().ToLower();
            }

            for (int i = _list.Count - 1; i >= 0; i--)
            {
                var res = _list[i];
                if (!typeInStr.Contains(res.Type))
                    _list.Remove(res);
            }
        }
    }
}
