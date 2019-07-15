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

namespace MG.Sonarr.Cmdlets.Series
{
    [Cmdlet(VerbsData.Update, "Series", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(SeriesResult))]
    public class UpdateSeries : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private CamelCasePropertyNamesContractResolver camel;
        private JsonSerializer cSerialize;
        private JsonSerializerSettings serializer;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public SeriesResult InputObject { get; set; }

        [Parameter(Mandatory = false)]
        public string NewPath { get; set; }

        [Parameter(Mandatory = false)]
        public bool Monitored { get; set; }

        [Parameter(Mandatory = false)]
        public bool UseSeasonFolder { get; set; }

        [Parameter(Mandatory = false)]
        public int QualityProfileId { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            camel = new CamelCasePropertyNamesContractResolver();
            cSerialize = new JsonSerializer
            {
                ContractResolver = camel
            };

            serializer = new JsonSerializerSettings
            {
                ContractResolver = camel,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DefaultValueHandling = DefaultValueHandling.Populate,
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Include,
                MissingMemberHandling = MissingMemberHandling.Error
            };
        }

        protected override void ProcessRecord()
        {
            var job = JObject.FromObject(this.InputObject, cSerialize);
            if (this.MyInvocation.BoundParameters.ContainsKey("NewPath"))
            {
                job["path"].Replace(this.NewPath);
            }

            if (this.MyInvocation.BoundParameters.ContainsKey("Monitored"))
            {
                job["monitored"].Replace(this.Monitored);
            }

            if (this.MyInvocation.BoundParameters.ContainsKey("UseSeasonFolder"))
            {
                job["seasonFolder"].Replace(this.UseSeasonFolder);
            }

            if (this.MyInvocation.BoundParameters.ContainsKey("QualityProfileId"))
            {
                job["qualityProfileId"].Replace(this.QualityProfileId);
            }

            string jsonBody = JsonConvert.SerializeObject(job, serializer);
            base.WriteDebug(jsonBody);

            string full = string.Format("/series/{0}", this.InputObject.Id);
            try
            {
                string outRes =_api.SonarrPut(full, jsonBody);
                if (!string.IsNullOrEmpty(outRes))
                {
                    var series = SonarrHttpClient.ConvertToSeriesResult(outRes);
                    base
                }

            }

        }

        #endregion

        #region METHODS


        #endregion
    }
}