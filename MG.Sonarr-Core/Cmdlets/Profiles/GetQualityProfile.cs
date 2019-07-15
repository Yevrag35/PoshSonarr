﻿using MG.Sonarr.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace MG.Sonarr.Cmdlets.Profiles
{
    [Cmdlet(VerbsCommon.Get, "QualityProfile", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ByProfileName")]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(QualityProfile))]
    public class GetProfile : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS


        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByProfileName")]
        public string[] Name { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByProfileId")]
        public int[] Id { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (this.ParameterSetName == "ByProfileName")
            {
                List<QualityProfile> profs = null;
                try
                {
                    string jsonStr = base.TryGetSonarrResult("/profile");
                    if (!string.IsNullOrEmpty(jsonStr))
                    {
                        profs = SonarrHttpClient.ConvertToSonarrResults<QualityProfile>(jsonStr, out bool iso);
                    }
                }
                catch (Exception e)
                {
                    base.WriteError(e, ErrorCategory.InvalidResult, "/profile");
                }

                if (profs != null && profs.Count > 0)
                {
                    if (this.Name != null && this.Name.Length > 0)
                    {
                        for (int n = 0; n < this.Name.Length; n++)
                        {
                            string name = this.Name[n];
                            var wcp = new WildcardPattern(name, WildcardOptions.IgnoreCase);
                            for (int p = 0; p < profs.Count; p++)
                            {
                                QualityProfile qp = profs[p];
                                if (wcp.IsMatch(qp.Name))
                                    base.WriteObject(qp);
                            }
                        }
                    }
                    else
                    {
                        base.WriteObject(profs, true);
                    }
                }
            }
            else
            {
                for (int i = 0; i < this.Id.Length; i++)
                {
                    int id = this.Id[i];
                    string full = string.Format("/profile/{0}", id);
                    try
                    {
                        string oneProf = base.TryGetSonarrResult(full);
                        if (!string.IsNullOrEmpty(oneProf))
                        {
                            var qp = SonarrHttpClient.ConvertToSonarrResult<QualityProfile>(oneProf);
                            base.WriteObject(qp);
                        }
                    }
                    catch (Exception e)
                    {
                        base.WriteError(e, ErrorCategory.InvalidResult, full);
                    }
                }
            }
        }

        #endregion

        #region METHODS


        #endregion
    }
}