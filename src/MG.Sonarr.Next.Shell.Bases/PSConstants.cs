namespace MG.Sonarr.Next.Shell
{
    public static class PSConstants
    {
        public const string PSET_EXPLICIT_ID = "ByExplicitId";
        public const string PSET_PIPELINE = "ByPipelineInput";

        public const string DEBUG = "Debug";
        public const string VERBOSE = "Verbose";
        public const string PREFERENCE = "Preference";
        public const string DEBUG_PREFERENCE = DEBUG + PREFERENCE;
        public const string VERBOSE_PREFERENCE = VERBOSE + PREFERENCE;
        public const string ERROR_ACTION = "ErrorAction";
        public const string ERROR_ACTION_PREFERENCE = ERROR_ACTION + PREFERENCE;
    }
}

