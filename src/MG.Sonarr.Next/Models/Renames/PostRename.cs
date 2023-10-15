using MG.Sonarr.Next.Json.Converters;
using MG.Sonarr.Next.Metadata;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Renames
{
    public sealed class PostRename
    {
        const string FILES = "files";
        private readonly SortedSet<int> _ids = null!;

        [JsonPropertyName(FILES)]
        [JsonConverter(typeof(EnumerableSerializer<int>))]
        public required IEnumerable<int> FileIds
        {
            get => _ids;
            init
            {
                if (value is null)
                {
                    return;
                }

                _ids = new(value);
            }
        }
        public string Name { get; } = CommandStrings.RENAME_FILES;
        public required int SeriesId { get; init; }

        public PostRename()
        {
        }

        [SetsRequiredMembers]
        private PostRename(int seriesId, int episodeFileId)
        {
            this.FileIds = null!;
            _ids = new() { episodeFileId };
            this.SeriesId = seriesId;
        }

        public static IEnumerable<PostRename> FromRenameObjects(IEnumerable<IRenameFilePipeable> renames)
        {
            SortedDictionary<int, PostRename> dict = new();
            foreach (IRenameFilePipeable ro in renames)
            {
                if (dict.TryGetValue(ro.SeriesId, out PostRename? existing))
                {
                    existing._ids.Add(ro.EpisodeFileId);
                }
                else
                {
                    dict.Add(ro.SeriesId, new PostRename(ro.SeriesId, ro.EpisodeFileId));
                }
            }

            return dict.Values;
        }

        const string FORMAT = "SeriesId = {0}; Ids = {{{1}}}";
        public override string ToString()
        {
            string idsStr = string.Join(',', _ids);
            return string.Format(FORMAT, this.SeriesId, idsStr);
        }
    }
}
