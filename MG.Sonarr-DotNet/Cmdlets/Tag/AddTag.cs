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
        private bool _isRemove;
        private Tag[] _removeTags;
        private int[] _ids;
        private string _ep;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public ISupportsTagUpdate InputObject { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "AddExistingTag")]
        public Tag[] AddExistingTag
        {
            get => _removeTags;
            set => _ids = value.Select(x => x.TagId).ToArray();
        }

        [Parameter(Mandatory = true, ParameterSetName = "AddExistingTagById")]
        public int[] AddExistingTagId
        {
            get => _ids;
            set => _ids = value;
        }

        [Parameter(Mandatory = true, ParameterSetName = "RemoveExistingTag")]
        public Tag[] RemoveExistingTag
        {
            get => _removeTags;
            set
            {
                _isRemove = true;
                _ids = value.Select(x => x.TagId).ToArray();
                _removeTags = value;
            }
        }

        [Parameter(Mandatory = true, ParameterSetName = "RemoveExistingTagById")]
        public int[] RemoveExistingTagId
        {
            get => _ids;
            set
            {
                _isRemove = true;
                _ids = value;
            }
        }

        [Parameter(Mandatory = true, ParameterSetName = "AddNewTag")]
        public string[] Tag { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            if (base.HasParameterSpecified(this, x => x.AddExistingTag))
            {
                this.AddExistingTagId = this.AddExistingTag.Select(x => x.TagId).ToArray();
                this.MyInvocation.BoundParameters.Remove("ExistingTag");
            }
        }

        protected override void ProcessRecord()
        {
            _ep = this.InputObject.GetEndpoint();
            if (base.FormatShouldProcess("Update", "Tags on Item: {0}", this.InputObject.Identifier))
            {
                int[] tagIds = null;
                if (base.HasParameterSpecified(this, x => x.Tag))
                {

                }
                else
                {

                }
            }
        }

        #endregion

        #region BACKEND METHODS
        private int[] CreateNewTags(string[] tags)
        {
            int[] tagIds = new int[tags.Length];
            for (int i = 0; i < tags.Length; i++)
            {

            }
            return tagIds;
        }

        #endregion
    }
}