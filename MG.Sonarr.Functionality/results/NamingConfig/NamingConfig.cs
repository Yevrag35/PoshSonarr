using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    public class NamingConfig
    {
        [JsonProperty("animeEpisodeFormat", Order = 6)]
        public string AnimeEpisodeFormat { get; private set; }

        [JsonProperty("dailyEpisodeFormat", Order = 5)]
        public string DailyEpisodeFormat { get; private set; }

        [JsonProperty("id")]
        public int Id { get; private set; }

        [JsonProperty("includeEpisodeTitle")]
        public bool IncludeEpisodeTitle { get; private set; }

        [JsonProperty("includeQuality")]
        public bool IncludeQuality { get; private set; }

        [JsonProperty("includeSeriesTitle")]
        public bool IncludeSeriesTitle { get; private set; }

        [JsonProperty("multiEpisodeStyle")]
        public int MultiEpisodeStyle { get; private set; }

        [JsonProperty("numberStyle")]
        public string NumberStyle { get; private set; }

        [JsonProperty("renameEpisodes")]
        public bool RenameEpisodes { get; private set; }

        [JsonProperty("replaceIllegalCharacters")]
        public bool ReplaceIllegalCharacters { get; private set; }

        [JsonProperty("replaceSpaces")]
        public bool RepalceSpaces { get; private set; }

        [JsonProperty("seasonFolderFormat")]
        public string SeasonFolderFormat { get; private set; }

        [JsonProperty("separator")]
        public string Separator { get; private set; }

        [JsonProperty("seriesFolderFormat")]
        public string SeriesFolderFormat { get; private set; }

        [JsonProperty("standardEpisodeFormat")]
        public string StandardEpisodeFormat { get; private set; }
    }
}
