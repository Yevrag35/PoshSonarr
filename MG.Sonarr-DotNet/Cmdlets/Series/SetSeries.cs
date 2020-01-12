using MG.Sonarr.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Security;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Set, "Series", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true, DefaultParameterSetName = "None")]
    [CmdletBinding(PositionalBinding = false)]
    [Alias("Update-Series")]
    [OutputType(typeof(SeriesResult))]
    public class SetSeries : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private List<Tag> _allCurrentTags;
        private TagTable _tagTable;

        private bool _clearTags;
        private bool _isMon;
        private bool _passThru;
        private bool _useFol;

        private const string SERIES_BY_ID = "/series/{0}";
        private const string TAG = "/tag";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public SeriesResult InputObject { get; set; }

        [Parameter(Mandatory = false)]
        public string NewPath { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter IsMonitored
        {
            get => _isMon;
            set => _isMon = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter UseSeasonFolder
        {
            get => _useFol;
            set => _useFol = value;
        }

        [Parameter(Mandatory = false)]
        public int QualityProfileId { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "EditingTags")]
        public object[] Tags { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ClearTags")]
        public SwitchParameter ClearTags
        {
            get => _clearTags;
            set => _clearTags = value;
        }

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
            if (base.HasParameterSpecified(this, x => x.Tags))
            {
                _allCurrentTags = base.SendSonarrListGet<Tag>(TAG);
                _tagTable = new TagTable(this.Tags);
            }
        }
        protected override void ProcessRecord()
        {
            this.MakeChangesBasedOnParameters();

            if (_tagTable != null)
                this.FormatTags(_tagTable);
            
            else if (_clearTags)
                this.InputObject.Tags.Clear();

            string endpoint = string.Format(SERIES_BY_ID, this.InputObject.SeriesId);
            base.WriteDebug(this.InputObject.ToJson());
            if (base.FormatShouldProcess("Set", "Series Id: {0}", this.InputObject.SeriesId))
            {
                SeriesResult putResult = base.SendSonarrPut<SeriesResult>(endpoint, this.InputObject);
                if (_passThru)
                    base.SendToPipeline(putResult);
            }
        }

        #endregion

        #region METHODS

        #region TAGS
        private Tag CreateNewTag(string label)
        {
            var pbp = new SonarrBodyParameters
            {
                { "label", label }
            };
            return base.SendSonarrPost<Tag>(TAG, pbp);
        }
        private void FormatTags(TagTable tt)
        {
            if (tt.IsSetting)
                this.SettingTags(tt);

            else
            {
                if (tt.IsAdding)
                {
                    this.AddingTags(tt);
                }
                if (tt.IsRemoving)
                    this.RemovingTags(tt);
            }
        }
        private void AddingTags(TagTable tt)
        {
            if (tt.HasAddById)
            {
                this.InputObject.Tags.UnionWith(tt.AddTagIds);
            }
            if (tt.AddTags != null && tt.AddTags.Length > 0)
            {
                foreach (string s in tt.AddTags)
                {
                    if (this.TryGetTagId(s, out int tagId))
                    {
                        this.InputObject.Tags.Add(tagId);
                    }
                    else
                    {
                        Tag newTag = this.CreateNewTag(s);
                        if (newTag != null)
                            this.InputObject.Tags.Add(newTag.TagId);
                    }
                }
            }
        }
        private void RemovingTags(TagTable tt)
        {
            if (tt.HasRemoveById)
            {
                this.InputObject.Tags.ExceptWith(tt.RemoveTagIds);
            }
            if (tt.RemoveTags != null && tt.RemoveTags.Length > 0)
            {
                foreach (string s in tt.RemoveTags)
                {
                    if (this.TryGetTagId(s, out int tagId))
                        this.InputObject.Tags.Remove(tagId);
                }
            }
        }
        private void SettingTags(TagTable tt)
        {
            this.InputObject.Tags.Clear();
            if (tt.HasSetById)
            {
                this.InputObject.Tags.UnionWith(tt.SetTagIds);
            }
            if (tt.SetTags != null && tt.SetTags.Length > 0)
            {
                foreach (string s in tt.SetTags)
                {
                    if (this.TryGetTagId(s, out int tagId))
                        this.InputObject.Tags.Add(tagId);

                    else
                    {
                        Tag newTag = this.CreateNewTag(s);
                        if (newTag != null)
                            this.InputObject.Tags.Add(newTag.TagId);
                    }
                }
            }
        }
        private bool TryGetTagId(string tagLabel, out int tagId)
        {
            tagId = 0;
            int? maybe = _allCurrentTags.Find(x => x.Label.Equals(tagLabel, StringComparison.InvariantCultureIgnoreCase))?.TagId;
            if (maybe.HasValue)
                tagId = maybe.Value;

            return maybe.HasValue;
        }

        #endregion

        private void MakeChangesBasedOnParameters()
        {
            if (base.HasAnyParameterSpecified(this, x => x.NewPath, x => x.IsMonitored,
                x => x.UseSeasonFolder, x => x.QualityProfileId))
            {
                if (base.HasParameterSpecified(this, x => x.NewPath))
                    this.InputObject.Path = this.NewPath;

                if (base.HasParameterSpecified(this, x => x.IsMonitored))
                    this.InputObject.IsMonitored = _isMon;

                if (base.HasParameterSpecified(this, x => x.QualityProfileId))
                    this.InputObject.QualityProfileId = this.QualityProfileId;

                if (base.HasParameterSpecified(this, x => x.UseSeasonFolder))
                    this.InputObject.UsingSeasonFolders = _useFol;
            }
        }

        #endregion
    }
}