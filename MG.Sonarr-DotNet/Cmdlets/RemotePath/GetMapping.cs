using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Security;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Mapping", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "PipelineRemotePath")]
    [OutputType(typeof(RemotePathMapping))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetMapping : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/remotepathmapping";
        private const string EP_WITH_ID = EP + "/{0}";

        private bool usingIds = false;
        private bool usingPaths = true;

        private List<int> _ids;
        private List<string> _paths;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ParameterSetName = "PipelineMappingId", ValueFromPipelineByPropertyName = true, DontShow = true)]
        public int MappingId { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByExplicitMappingId")]
        public int[] Id { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = "PipelineRemotePath", ValueFromPipelineByPropertyName = true, DontShow = true)]
        [Alias("FullName")]
        public string InputPath { get; set; }

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByExplicitRemotePath")]
        public string[] Path { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            if (base.HasParameterSpecified(this, x => x.Id))
            {
                _ids = new List<int>(this.Id);
                usingIds = true;
                usingPaths = false;
            }

            else if (base.HasParameterSpecified(this, x => x.Path))
            {
                _paths = new List<string>(this.Path);
            }

            else
            {
                _ids = new List<int>();
                _paths = new List<string>();
            }
        }

        protected override void ProcessRecord()
        {
            if (base.HasParameterSpecified(this, x => x.MappingId))
            {
                _ids.Add(this.MappingId);
                usingIds = true;
            }
            else if (base.HasParameterSpecified(this, x => x.InputPath))
            {
                _paths.Add(this.InputPath);
            }
        }

        protected override void EndProcessing() => base.WriteObject(this.Filter(), true);

        //protected override void ProcessRecord()
        //{
        //    if (this.ParameterSetName == "ByRemotePathName")
        //    {
        //        string jsonRes = base.TryGetSonarrResult(EP);
        //        if (!string.IsNullOrEmpty(jsonRes))
        //        {
        //            List<RemotePathMapping> mappings = SonarrHttp.ConvertToSonarrResults<RemotePathMapping>(jsonRes);
        //            if (this.MyInvocation.BoundParameters.ContainsKey("RemotePath"))
        //            {
        //                var wcp = new WildcardPattern(this.RemotePath);
        //                base.WriteObject(mappings.FindAll(x => wcp.IsMatch(x.RemotePath)), true);
        //            }
        //            else
        //                base.WriteObject(mappings, true);
        //        }
        //    }
        //    else if (this.MyInvocation.BoundParameters.ContainsKey("MappingId"))
        //    {
        //        for (int i = 0; i < this.MappingId.Length; i++)
        //        {
        //            string jsonRes = base.TryGetSonarrResult(string.Format(EP_WITH_ID, this.MappingId[i]));
        //            if (!string.IsNullOrEmpty(jsonRes))
        //            {
        //                RemotePathMapping mapping = SonarrHttp.ConvertToSonarrResult<RemotePathMapping>(jsonRes);
        //                base.WriteObject(mapping);
        //            }
        //        }
        //    }
        //}

        #endregion

        #region BACKEND METHODS
        private IEnumerable<RemotePathMapping> Filter()
        {
            if (usingIds)
            {
                return this.GetRemotePathMappingsByIds();
            }
            else
            {
                return this.GetRemotePathMappingsByPaths();
            }
        }

        private IEnumerable<RemotePathMapping> GetRemotePathMappingsByIds()
        {
            foreach (int oneId in _ids)
            {
                string endpoint = string.Format(EP_WITH_ID, oneId);
                RemotePathMapping rpm = base.SendSonarrGet<RemotePathMapping>(endpoint);
                if (rpm != null)
                    yield return rpm;
            }
        }
        private List<RemotePathMapping> GetRemotePathMappingsByPaths()
        {
            List<RemotePathMapping> allMappings = base.SendSonarrListGet<RemotePathMapping>(EP);
            if (base.HasParameterSpecified(this, x => x.Path))
            {
                IEnumerable<WildcardPattern> patterns = this.Path
                    .Select(x => new WildcardPattern(x, WildcardOptions.IgnoreCase));

                return allMappings.FindAll(rpm => patterns.Any(wp => wp.IsMatch(rpm.RemotePath)));
            }
            else
            {
                return allMappings;
            }
        }

        #endregion
    }
}