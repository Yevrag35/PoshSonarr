using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// <para type="description">Represents a response object from "/filesystem".</para>
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class FileSystem : BaseResult, IAdditionalInfo
    {
        [JsonExtensionData]
        private IDictionary<string, JToken> _data;

        [JsonProperty("directories")]
        public List<SonarrDirectory> Directories { get; private set; }

        [JsonProperty("files")]
        public List<SonarrFile> Files { get; private set; }

        [JsonIgnore]
        public bool HasFiles => this.Files != null && this.Files.Count > 0;

        [JsonIgnore]
        public bool HasFolders => this.Directories != null && this.Directories.Count > 0;

        [JsonConstructor]
        public FileSystem()
        {
            this.Directories = new List<SonarrDirectory>();
            this.Files = new List<SonarrFile>();
        }

        public IDictionary GetAdditionalInfo() => _data as IDictionary;

        private void Sort()
        {
            if (this.HasFiles)
                this.Files.Sort();

            if (this.HasFolders)
                this.Directories.Sort();
        }

        public List<FileSystemEntry> ToAllList()
        {
            this.Sort();
            var list = new List<FileSystemEntry>(this.Directories.Count + this.Files.Count);
            list.AddRange(this.Directories);
            list.AddRange(this.Files);
            return list;
        }
    }

    public abstract class FileSystemEntry : BaseResult, IComparable<FileSystemEntry>
    {
        [JsonProperty("size")]
        private protected long _size;

        [JsonProperty("lastModified")]
        public DateTime LastModified { get; private protected set; }

        [JsonProperty("name")]
        public string Name { get; private protected set; }

        [JsonProperty("path")]
        [JsonConverter(typeof(PathConverter))]
        public string Path { get; private protected set; }

        [JsonIgnore]
        public abstract string Type { get; }

        public int CompareTo(FileSystemEntry other) => this.Path.CompareTo(other.Path);
    }

    /// <summary>
    /// <para type="description">Represents a repsonse object from a "/filesystem" request as an individual directory result.</para>
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class SonarrDirectory : FileSystemEntry
    {
        [JsonProperty("type")]
        [JsonConverter(typeof(SonarrStringEnumConverter))]
        public override string Type => "Folder";
    }

    public class SonarrFile : FileSystemEntry, IComparable<SonarrFile>
    {
        private const string GB = "{0} GB";
        private const string KB = "{0} KB";
        private const string MB = "{0} MB";
        private const double ONE_GB = 1073741824.00d;
        private const double ONE_KB = 1024.00d;
        private const double ONE_MB = 1048576.00d;

        [JsonProperty("extension")]
        public string Extension { get; private protected set; }

        [JsonIgnore]
        public long Size => base._size;

        [JsonIgnore]
        public string SizeString { get; private set; }

        [JsonProperty("type")]
        [JsonConverter(typeof(SonarrStringEnumConverter))]
        public override string Type => "File";

        public int CompareTo(SonarrFile other) => this.Path.CompareTo(other.Path);

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            double rounder = ONE_MB;
            string format = MB;

            if (_size <= 534774L)
            { 
                rounder = ONE_KB;
                format = KB;
            }
            else if (_size > 1258291200L)
            {
                rounder = ONE_GB;
                format = GB;
            }
            double roundedSize = Math.Round(base._size / rounder, 2);
            this.SizeString = string.Format(format, roundedSize);
        }
    }
}
