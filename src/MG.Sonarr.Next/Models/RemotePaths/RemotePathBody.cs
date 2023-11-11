using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.RemotePaths
{
    public sealed class RemotePathBody
    {
        string? _hostName;
        string? _localPath;
        string? _remotePath;

        [JsonPropertyOrder(0)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Id { get; set; }

        [JsonPropertyOrder(1)]
        public string Host
        {
            get => _hostName ??= string.Empty;
            set => _hostName = value;
        }
        [JsonPropertyOrder(2)]
        public string LocalPath
        {
            get => _localPath ??= string.Empty;
            set => _localPath = value;
        }
        [JsonPropertyOrder(3)]
        public string RemotePath
        {
            get => _remotePath ??= string.Empty;
            set => _remotePath = value;
        }
    }
}

