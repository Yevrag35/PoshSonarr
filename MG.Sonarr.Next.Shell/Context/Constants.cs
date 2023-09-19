using System.CodeDom;

namespace MG.Sonarr.Next.Shell.Context
{
    public static class Constants
    {
        public const string META_PROPERTY_NAME = "MetadataTag";
        public const char META_PREFIX = '#';

        public const string CALENDAR = "Calendar";
        public const string EPISODE = "Episode";
        public const string EPISODE_FILE = "EpisodeFile";
        public const string LABEL = "Label";
        public const string NAME = "Name";
        public const string SERIES = "Series";
        public const string TAG = "Tag";
        public const string TITLE = "Title";

        public const string DEBUG = "Debug";
        public const string VERBOSE = "Verbose";
        public const string PREFERENCE = "Preference";
        public const string DEBUG_PREFERENCE = DEBUG + PREFERENCE;
        public const string VERBOSE_PREFERENCE = VERBOSE + PREFERENCE;

    }
}
