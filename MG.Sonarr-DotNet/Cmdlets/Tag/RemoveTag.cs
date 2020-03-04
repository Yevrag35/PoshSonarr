using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Remove, "Tag", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true, 
        DefaultParameterSetName = "None")]
    [CmdletBinding(PositionalBinding = false)]
    public class RemoveTag : TagCmdlet
    {
        #region FIELDS/CONSTANTS
        private bool _force;
        private bool _passThru;

        private bool _clearAll;
        private bool _ioSet;
        private HashSet<int> _ids;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipeline = true, DontShow = true, ParameterSetName = "TagViaPipeline")]
        public Tag InputObject { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "RemoveFromObjectWithAll")]
        public SwitchParameter ClearAll
        {
            get => _clearAll;
            set => _clearAll = value;
        }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "None")]
        [Parameter(Mandatory = true, ParameterSetName = "RemoveFromObjectWithIds")]
        public int[] Id
        {
            get => null;
            set => _ids = new HashSet<int>(value);
        }

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "RemoveFromObjectWithIds")]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "RemoveFromObjectWithAll")]
        public ISupportsTagUpdate RemoveFrom { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Force
        {
            get => _force;
            set => _force = value;
        }

        [Parameter(Mandatory = false, ParameterSetName = "RemoveFromObjectWithIds")]
        [Parameter(Mandatory = false, ParameterSetName = "RemoveFromObjectWithAll")]
        public SwitchParameter PassThru
        {
            get => _passThru;
            set => _passThru = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            if ( ! this.ContainsParameter(x => x.Id))
                _ids = new HashSet<int>();
        }

        protected override void ProcessRecord()
        {
            if (this.ContainsParameter(x => x.InputObject))
            {
                _ids.Add(this.InputObject.Id);
                _ioSet = true;
            }
            else if ( ! this.ContainsAnyParameters(x => x.InputObject, x => x.RemoveFrom))
                _ioSet = true;

            else if (this.ContainsParameter(x => x.RemoveFrom))
            {
                string msg = null;
                if (_clearAll)
                    msg = "Remove All Tags";

                else
                    msg = string.Format("Remove Tag - {0}", string.Join(", ", _ids));

                if (_force || base.FormatShouldProcess(msg, "Item - {0}", this.RemoveFrom.Id))
                {
                    string ep = this.RemoveFrom.GetEndpoint();
                    if (_clearAll)
                        this.RemoveFrom.Tags.Clear();

                    else
                        this.RemoveFrom.Tags.ExceptWith(_ids);

                    object modified = base.SendSonarrPut(ep, this.RemoveFrom, this.RemoveFrom.GetType());
                    if (_passThru)
                        base.SendToPipeline(modified);
                }
            }
        }

        protected override void EndProcessing()
        {
            if (_ioSet && _ids.Count > 0)
            {
                string msg = string.Format("Tag - {0}", string.Join(", ", _ids));
                if (_force || base.FormatShouldProcess("Remove", msg))
                {
                    foreach (int id in _ids)
                    {
                        Context.TagManager.Remove(id);
                    }
                }
            }
        }

        #endregion

        //private string GetMessage(HashSet<int> ids)
        //{
        //    if (_ids == null || _ids.Count <= 0)
        //        return "All Tags";

        //    else
        //        return string.Format("Tag - {0}", string.Join(", ", ids));
        //}

        //private void RemoveTagsFromObject(HashSet<int> ids) => this.RemoveFrom.Tags.ExceptWith(ids);
    }
}