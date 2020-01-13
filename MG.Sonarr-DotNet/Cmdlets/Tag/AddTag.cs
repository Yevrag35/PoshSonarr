using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Add, "Tag", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true, DefaultParameterSetName = "AddNewTag")]
    [CmdletBinding(PositionalBinding = false)]
    public class AddTag : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string TAG_EP = "/tag";
        private string _ep;
        private bool _isRemove;
        private int[] _ids;
        private bool _passThru;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public ISupportsTagUpdate InputObject { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "AddExistingTag")]
        public Tag[] ExistingTag
        {
            get => null;
            set => _ids = value.Select(x => x.TagId).ToArray();
        }

        [Parameter(Mandatory = true, ParameterSetName = "AddExistingTagById")]
        public int[] ExistingTagId
        {
            get => null;
            set => _ids = value;
        }

        //[Parameter(Mandatory = true, ParameterSetName = "RemoveExistingTag")]
        //public Tag[] RemoveExistingTag
        //{
        //    get => null;
        //    set
        //    {
        //        _isRemove = true;
        //        _ids = value.Select(x => x.TagId).ToArray();
        //    }
        //}

        //[Parameter(Mandatory = true, ParameterSetName = "RemoveExistingTagById")]
        //public int[] RemoveExistingTagId
        //{
        //    get => _ids;
        //    set
        //    {
        //        _isRemove = true;
        //        _ids = value;
        //    }
        //}

        [Parameter(Mandatory = true, ParameterSetName = "AddNewTag")]
        public string[] Tag { get; set; }

        [Parameter(Mandatory = false)]
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
            if (base.HasParameterSpecified(this, x => x.ExistingTag))
            {
                this.ExistingTagId = this.ExistingTag.Select(x => x.TagId).ToArray();
                this.MyInvocation.BoundParameters.Remove("ExistingTag");
            }
        }

        protected override void ProcessRecord()
        {
            _ep = this.InputObject.GetEndpoint();
            if (base.FormatShouldProcess("Add", "Tags on Item: {0}", this.InputObject.Identifier))
            {
                //int[] tagIds = null;
                if (base.HasParameterSpecified(this, x => x.Tag))
                {
                    _ids = this.CreateNewTags(this.Tag);
                }
                this.InputObject.AddTags(_ids);
                base.WriteDebug(this.InputObject.ToJson());
                object updated = base.SendSonarrPut(_ep, this.InputObject, this.InputObject.GetType());
                if (_passThru)
                    base.SendToPipeline(updated);
            }
        }

        #endregion

        #region BACKEND METHODS
        private int[] CreateNewTags(string[] tags)
        {
            int[] tagIds = new int[tags.Length];
            for (int i = 0; i < tags.Length; i++)
            {
                var postTag = new TagNew(tags[i]);
                Tag newTag = base.SendSonarrPost<Tag>(TAG_EP, postTag);
                tagIds[i] = newTag.TagId;
            }
            return tagIds;
        }

        #endregion
    }
}