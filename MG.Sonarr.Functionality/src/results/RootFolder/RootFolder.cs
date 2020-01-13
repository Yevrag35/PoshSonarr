using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// The class that defines a response from the "/rootfolder" endpoint.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class RootFolder : BaseResult, IComparable<RootFolder>, IEquatable<RootFolder>
    {
        private const decimal GB_DECIMAL = 1073741824.00M;

        [JsonProperty("freeSpace")]
        public long FreeSpace { get; private set; }

        [JsonIgnore]
        public decimal FreeSpaceInGB { get; private set; }

        [JsonProperty("path")]
        public string Path { get; private set; }

        [JsonProperty("id")]
        public int RootFolderId { get; private set; }

        [JsonProperty("totalSpace")]
        public long TotalSpace { get; private set; }

        [JsonIgnore]
        public decimal TotalSpaceInGB { get; private set; }

        [JsonProperty("unmappedFolders")]
        public List<UnmappedFolder> UnmappedFolders { get; private set; } = new List<UnmappedFolder>();

        public int CompareTo(RootFolder other) => this.RootFolderId.CompareTo(other.RootFolderId);
        public bool Equals(RootFolder other) => this.RootFolderId.Equals(other.RootFolderId);
        public override int GetHashCode() => this.RootFolderId.GetHashCode();

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            this.UnmappedFolders.Sort();

            if (this.FreeSpace > 0)
                this.FreeSpaceInGB = this.RoundToGB(this.FreeSpace);

            if (this.TotalSpace > 0)
                this.TotalSpaceInGB = this.RoundToGB(this.TotalSpace);
        }
        private decimal RoundToGB(long sizeInBytes) => Math.Round(Convert.ToDecimal(sizeInBytes) / GB_DECIMAL, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// The class that defines an unmapped folder in Sonarr.
    /// </summary>
    public class UnmappedFolder : BaseResult, IComparable<UnmappedFolder>
    {
        public string Name { get; set; }
        public string Path { get; set; }

        public int CompareTo(UnmappedFolder other) => this.Name.CompareTo(other.Name);
    }
}
