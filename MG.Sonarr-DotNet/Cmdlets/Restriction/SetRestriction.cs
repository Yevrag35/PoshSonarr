using MG.Posh.Extensions.Bound;
using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Set, "Restriction", ConfirmImpact = ConfirmImpact.None, SupportsShouldProcess = true,
        DefaultParameterSetName = "ByInputRestrictionAddRemove")]
    [OutputType(typeof(Restriction))]
    [CmdletBinding(PositionalBinding = false)]
    public class SetRestriction : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private bool _passThru;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ParameterSetName = "ByInputRestrictionAddRemove", ValueFromPipeline = true, DontShow = true)]
        [Parameter(Mandatory = true, ParameterSetName = "ByInputRestrictionReplace", ValueFromPipeline = true, DontShow = true)]
        public Restriction InputObject { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByRestrictionIdAddRemove")]
        [Parameter(Mandatory = true, ParameterSetName = "ByRestrictionIdReplace")]
        public int Id { get; set; }

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByInputRestrictionAddRemove")]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByRestrictionIdAddRemove")]
        [Alias("Ignored")]
        public AddRemoveHashtable IgnoredTerms { get; set; }

        [Parameter(Mandatory = false, Position = 1, ParameterSetName = "ByInputRestrictionAddRemove")]
        [Parameter(Mandatory = false, Position = 1, ParameterSetName = "ByRestrictionIdAddRemove")]
        [Alias("Required")]
        public AddRemoveHashtable RequiredTerms { get; set; }

        [Parameter(Mandatory = true, Position = 2, ParameterSetName = "ByInputRestrictionReplace")]
        [Parameter(Mandatory = true, Position = 2, ParameterSetName = "ByRestrictionIdReplace")]
        [Alias("Replace")]
        public ReplaceHashtable ReplaceTerms { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru
        {
            get => _passThru;
            set => _passThru = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (!this.ParameterSetName.Contains("ByInputRestriction"))
            {
                this.InputObject = base.SendSonarrGet<Restriction>(string.Format(GetRestriction.EP_ID, this.Id));
            }

            if (this.ContainsParameter(x => x.IgnoredTerms))
            {
                if (this.IgnoredTerms.AddTerms != null && this.IgnoredTerms.AddTerms.Length > 0)
                    this.InputObject.Ignored.Add(this.IgnoredTerms.AddTerms);

                if (this.IgnoredTerms.RemoveTerms != null && this.IgnoredTerms.RemoveTerms.Length > 0)
                    this.InputObject.Ignored.Remove(this.IgnoredTerms.RemoveTerms);
            }

            if (this.ContainsParameter(x => x.RequiredTerms))
            {
                if (this.RequiredTerms.AddTerms != null && this.RequiredTerms.AddTerms.Length > 0)
                    this.InputObject.Required.Add(this.RequiredTerms.AddTerms);

                if (this.RequiredTerms.RemoveTerms != null && this.RequiredTerms.RemoveTerms.Length > 0)
                    this.InputObject.Required.Remove(this.RequiredTerms.RemoveTerms);
            }

            if (this.ContainsParameter(x => x.ReplaceTerms))
                this.MergeChanges(this.ReplaceTerms);

            if (base.ShouldProcess(string.Format(NewRestriction.SHOULD_MSG, this.InputObject.Ignored.ToJson(), 
                this.InputObject.Required.ToJson()), "Set"))
            {
                Restriction restriction = base.SendSonarrPut<Restriction>(GetRestriction.EP, this.InputObject);
                if (_passThru)
                    base.SendToPipeline(restriction);
            }
        }

        #endregion

        #region BACKEND METHODS
        private void MergeChanges(ReplaceHashtable addRemove)
        {
            this.InputObject.Ignored.MergeCollections(addRemove.Ignored);
            this.InputObject.Required.MergeCollections(addRemove.Required);
        }

        #endregion
    }
}