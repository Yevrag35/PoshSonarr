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
    [Cmdlet(VerbsCommon.Set, "Restriction", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true,
        DefaultParameterSetName = "ByInputRestrictionAddRemove")]
    [OutputType(typeof(Restriction))]
    [Alias("Update-Restriction")]
    [CmdletBinding(PositionalBinding = false)]
    public class SetRestriction : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private TermTable _ignoredTerms;
        private TermTable _requiredTerms;
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
        [AllowNull()]
        [Alias("Ignored")]
        public object IgnoredTerms
        {
            get => _ignoredTerms;
            set
            {
                _ignoredTerms = new TermTable();
                if (value == null)
                    _ignoredTerms.Set.Add(string.Empty);

                else
                    _ignoredTerms.Process(value);
            }
        }

        [Parameter(Mandatory = false, Position = 1, ParameterSetName = "ByInputRestrictionAddRemove")]
        [Parameter(Mandatory = false, Position = 1, ParameterSetName = "ByRestrictionIdAddRemove")]
        [AllowNull()]
        [Alias("Required")]
        public object RequiredTerms
        {
            get => _requiredTerms;
            set
            {
                _requiredTerms = new TermTable();
                if (value == null)
                    _requiredTerms.Set.Add(string.Empty);

                else
                    _requiredTerms.Process(value);
            }
        }

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

            if (base.FormatShouldProcess("Set", "Restriction Id: {0}", this.InputObject.Id))
            {
                if (this.ContainsParameter(x => x.IgnoredTerms))
                {
                    _ignoredTerms.ModifyObject(this.InputObject.Ignored);
                }
                if (this.ContainsParameter(x => x.RequiredTerms))
                {
                    _requiredTerms.ModifyObject(this.InputObject.Required);
                }
                Restriction restriction = base.SendSonarrPut<Restriction>(GetRestriction.EP, this.InputObject);
                if (_passThru)
                    base.SendToPipeline(restriction);
            }
        }

        #endregion
    }
}