using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using MG.Sonarr.Functionality.Strings;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class QualityProfileNew : BaseResult, IGetEndpoint
    {
        //private IEqualityComparer<string> _comparer;

        [JsonProperty("items", Order = 3)]
        public AllowedQualityCollection AllowedQualities { get; private set; }

        [JsonProperty("cutoff", Order = 2)]
        [JsonConverter(typeof(QualityConverter))]
        private IQuality _cutoff;

        [JsonIgnore]
        public string Cutoff => _cutoff?.Name;

        [JsonIgnore]
        public int? CutOffId => _cutoff?.Id;

        [JsonProperty("language", Order = 4)]
        [JsonConverter(typeof(SonarrStringEnumConverter))]
        public ProfileLanugage Language { get; set; } = ProfileLanugage.English;

        [JsonProperty("name", Order = 1)]
        public string Name { get; set; }

        public QualityProfileNew()
        {
        }

        public string GetEndpoint() => ApiEndpoints.Profile;

        public void ApplyAllowables(IEnumerable<IQuality> allowables)
        {
            this.AllowedQualities.Allow(allowables);
        }
        public void ApplyAllowablesByName(params string[] names)
        {
            this.AllowedQualities.Allow(names);
        }
        public void ApplyAllowablesById(params int[] ids)
        {
            this.AllowedQualities.Allow(ids);
        }
        public void ApplyDisallowables(IEnumerable<IQuality> disallowables)
        {
            if (_cutoff != null)
                disallowables = disallowables.Where(x => !x.Id.Equals(_cutoff.Id));

            this.AllowedQualities.Disallow(disallowables);
        }
        public void ApplyDisallowables(IEnumerable<string> names)
        {
            bool cutOffNull = _cutoff == null;

            foreach (string name in names)
            {
                if (cutOffNull || _cutoff.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                    continue;

                this.AllowedQualities.Disallow(name);
            }
        }

        public void PopulateQualities(IEnumerable<IQuality> qualities)
        {
            this.AllowedQualities = new AllowedQualityCollection(qualities.Select(x => AllowedQuality.FromQuality(x, false)));
            if (this._cutoff != null)
            {
                this.AllowedQualities[this._cutoff.Name].Allowed = true;
            }
        }

        public void SetCutoff(IQuality quality) => _cutoff = quality;
    }

    /// <summary>
    /// The class that defines a response from the "/profile" endpoint.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class QualityProfile : QualityProfileNew, IComparable<QualityProfile>, IEquatable<QualityProfile>
    {
        [JsonProperty("id", Order = 5)]
        public int Id { get; internal set; }

        [JsonConstructor]
        private QualityProfile() : base() { }

        public int CompareTo(QualityProfile other) => this.Id.CompareTo(other.Id);
        public bool Equals(QualityProfile other) => this.Id.Equals(other.Id);
    }
}
