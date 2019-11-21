using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// The class defining a response from the "/episodefile" endpoint.
    /// </summary>
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class EpisodeFile : BaseResult
    {
        /// <summary>
        /// The date in which this <see cref="EpisodeFile"/> was added to the database.  This can be null.
        /// </summary>
        [JsonProperty("dateAdded")]
        public DateTime? DateAdded { get; set; }

        /// <summary>
        /// The of ID of the <see cref="EpisodeFile"/>.
        /// </summary>
        [JsonProperty("Id")]
        public long EpisodeFileId { get; set; }

        /// <summary>
        /// The media details of the <see cref="EpisodeFile"/>.
        /// </summary>
        [JsonProperty("mediaInfo")]
        public MediaInfo MediaInfo { get; set; }

        /// <summary>
        /// The name (path) of the file downloaded.
        /// </summary>
        [JsonProperty("originalFilePath")]
        public string OriginalFilePath { get; set; }

        /// <summary>
        /// The current path of this <see cref="EpisodeFile"/>.
        /// </summary>
        [JsonProperty("path")]
        public string Path { get; set; }

        /// <summary>
        /// The quality details of the <see cref="EpisodeFile"/>.
        /// </summary>
        [JsonProperty("quality")]
        public EpisodeFileQuality Quality { get; set; }

        /// <summary>
        /// Indicates the current version of the <see cref="EpisodeFile"/> does not meet the specified quality standards, and can be replaced when one is found.
        /// </summary>
        [JsonProperty("qualityCutoffNotMet")]
        public bool QualityCutoffNotMet { get; set; }

        /// <summary>
        /// The relative path of the <see cref="EpisodeFile"/>.
        /// </summary>
        [JsonProperty("relativePath")]
        public string RelativePath { get; set; }

        [JsonProperty("sceneName")]
        public string SceneName { get; set; }

        /// <summary>
        /// The series ID that the <see cref="EpisodeResult"/> of the <see cref="EpisodeFile"/> is apart of.
        /// </summary>
        [JsonProperty("seriesId")]
        public int SeriesId { get; set; }

        /// <summary>
        /// The season number for this <see cref="EpisodeFile"/>.
        /// </summary>
        [JsonProperty("seasonNumber")]
        public int SeasonNumber { get; set; }

        /// <summary>
        /// The episode file size in bytes.
        /// </summary>
        [JsonProperty("size")]
        public long Size { get; set; }
    }

    /// <summary>
    /// The class defining the quality of a specific <see cref="EpisodeFile"/>.
    /// </summary>
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class EpisodeFileQuality : BaseResult
    {
        /// <summary>
        /// The details of this Quality class of the specified <see cref="EpisodeFile"/>.
        /// </summary>
        [JsonProperty("quality")]
        public QualityDetails Quality { get; set; }
        /// <summary>
        /// Represents revision details about the Quality of the specified <see cref="EpisodeFile"/>.
        /// </summary>
        [JsonProperty("revision")]
        public Revision Revision { get; set; }
    }
}
