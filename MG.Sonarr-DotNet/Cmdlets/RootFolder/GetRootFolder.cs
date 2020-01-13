using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "RootFolder", ConfirmImpact = ConfirmImpact.None)]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(RootFolder))]
    public class GetRootFolder : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/rootfolder";
        private const string EP_ID = EP + "/{0}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0)]
        public int[] Id { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            IEnumerable<RootFolder> getTheseFolders = this.GetRootFolders(this.Id);
            base.SendToPipeline(getTheseFolders);
        }

        #endregion

        #region METHODS
        private IEnumerable<RootFolder> GetRootFolders(params int[] ids)
        {
            if (ids == null || ids.Length <= 0)
            {
                return this.GetAllRootFolders();
            }
            else
            {
                return this.GetSpecificRootFolders(ids);
            }
        }

        private List<RootFolder> GetAllRootFolders()
        {
            return base.SendSonarrListGet<RootFolder>(EP);
        }

        private IEnumerable<RootFolder> GetSpecificRootFolders(params int[] ids)
        {
            foreach (int id in ids)
            {
                string epWithId = string.Format(EP_ID, id);
                RootFolder oneRf = base.SendSonarrGet<RootFolder>(epWithId);
                if (oneRf != null)
                    yield return oneRf;
            }
        }

        #endregion
    }
}