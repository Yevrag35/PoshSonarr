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
    [Cmdlet(VerbsCommon.Set, "Series", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [CmdletBinding(PositionalBinding = false)]
    [Alias("Update-Series")]
    [OutputType(typeof(SeriesResult))]
    public class SetSeries : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private CamelCasePropertyNamesContractResolver camel;
        private JsonSerializer cSerialize;
        private JsonSerializerSettings serializer;
        private List<Tag> _allCurrentTags;
        private TagTable _tagTable;

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

        [Parameter(Mandatory = false)]
        public object[] Tags { get; set; }

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

            string endpoint = string.Format(SERIES_BY_ID, this.InputObject.SeriesId);

            if (base.FormatShouldProcess("Set", "Series Id: {0}", this.InputObject.SeriesId))
            {
                SeriesResult putResult = base.SendSonarrPut<SeriesResult>(endpoint, this.InputObject);
                if (_passThru)
                    base.SendToPipeline(putResult);
            }
        }

        #endregion

        #region METHODS
        private void AddingTags()

        private Tag CreateNewTag(string label)
        {
            var pbp = new SonarrBodyParameters
            {
                { "label", label }
            };
            return base.SendSonarrPost<Tag>(TAG, pbp);
        }

        private void FormatTags()
        {
            if (_tagTable.IsAdding)
            {
                if (_tagTable.HasAddById)
                {
                    this.InputObject.Tags.UnionWith(_tagTable.AddTagIds);
                }
                else
                {
                    foreach (string s in _tagTable.AddTags)
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
        }

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

        private bool TryGetTagId(string tagLabel, out int tagId)
        {
            tagId = 0;
            int? maybe = _allCurrentTags.Find(x => x.Label.Equals(tagLabel, StringComparison.InvariantCultureIgnoreCase))?.TagId;
            if (maybe.HasValue)
                tagId = maybe.Value;

            return maybe.HasValue;
        }

        #endregion

        //protected override void BeginProcessing()
        //{
        //    base.BeginProcessing();
        //    camel = new CamelCasePropertyNamesContractResolver();
        //    cSerialize = new JsonSerializer
        //    {
        //        ContractResolver = camel
        //    };

        //    serializer = new JsonSerializerSettings
        //    {
        //        ContractResolver = camel,
        //        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        //        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        //        DefaultValueHandling = DefaultValueHandling.Populate,
        //        Formatting = Formatting.Indented,
        //        NullValueHandling = NullValueHandling.Include,
        //        MissingMemberHandling = MissingMemberHandling.Error
        //    };
        //}

        //protected override void ProcessRecord()
        //{
        //    var job = JObject.FromObject(this.InputObject, cSerialize);
        //    if (this.MyInvocation.BoundParameters.ContainsKey("NewPath"))
        //    {
        //        job["path"].Replace(this.NewPath);
        //    }

        //    if (this.MyInvocation.BoundParameters.ContainsKey("Monitored"))
        //    {
        //        job["monitored"].Replace(this.Monitored);
        //    }

        //    if (this.MyInvocation.BoundParameters.ContainsKey("UseSeasonFolder"))
        //    {
        //        job["seasonFolder"].Replace(this.UseSeasonFolder);
        //    }

        //    if (this.MyInvocation.BoundParameters.ContainsKey("QualityProfileId"))
        //    {
        //        job["qualityProfileId"].Replace(this.QualityProfileId);
        //    }

        //    string jsonBody = JsonConvert.SerializeObject(job, serializer);

        //    string full = string.Format("/series/{0}", this.InputObject.SeriesId);
        //    string outRes = base.TryPutSonarrResult(full, jsonBody);
        //    if (!string.IsNullOrEmpty(outRes))
        //    {
        //        SeriesResult series = SonarrHttp.ConvertToSeriesResult(outRes);
        //        base.WriteObject(series);
        //    }
        //}   
    }
}