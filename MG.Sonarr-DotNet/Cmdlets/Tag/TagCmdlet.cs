using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Extensions;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    public abstract class TagCmdlet : BaseIdEndpointCmdlet
    {
        #region FIELDS/CONSTANTS
        protected override string Endpoint => "/tag";

        private const string PARAM_ID = "id";
        private const string PARAM_LBL = "label";

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        #endregion

        #region BACKEND METHODS
        protected IEnumerable<Tag> GetTagById(IEnumerable<int> ids)
        {
            foreach (int id in ids)
            {
                Tag oneTag = base.SendSonarrGet<Tag>(base.FormatWithId(id));
                if (oneTag != null)
                    yield return oneTag;
            }
        }

        private SonarrBodyParameters GetBody(int tagId, string tagLabel)
        {
            return new SonarrBodyParameters
            {
                { PARAM_ID, tagId },
                { PARAM_LBL, tagLabel }
            };
        }

        protected Tag NewTag(string label)
        {
            int tagId = Context.TagManager.AddNew(label);
            return Context.TagManager.GetTag(tagId);
        }

        protected Tag RenameTag(int tagId, string newLabel) => Context.TagManager.Edit(tagId, newLabel);

        protected bool RemoveTag(int tagId) => Context.TagManager.Remove(tagId);
        protected bool RemoveTag(string tagLabel, StringComparison comparison)
        {
            bool result = false;
            if (Context.TagManager.TryGetTag(tagLabel, comparison, out Tag outTag))
            {
                result = Context.TagManager.Remove(outTag.Id);
            }
            return result;
        }

        protected bool TryGetAllTags(out TagCollection outTags)
        {
            outTags = Context.TagManager.AllTags;
            return outTags != null && outTags.Count > 0;
        }

        #endregion
    }
}