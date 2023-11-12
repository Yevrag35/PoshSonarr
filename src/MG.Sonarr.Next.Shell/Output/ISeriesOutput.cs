namespace MG.Sonarr.Next.Shell.Output
{
    internal interface ISeriesOutput : IIdTagOuptut
    {
        DateTimeOffset Added { get; }
        object[] AlternateTitles { get; }
        string CleanTitle { get; }
        bool Ended { get; }
        string[] Genres { get; }
        object[] Images { get; }
        bool IsMonitored { get; }
        int LanguageProfileId { get; }
        string Network { get; }
        string Overview { get; }
        string Path { get; }
        int QualityProfileId { get; }
        object Ratings { get; }
        string RootFolderPath { get; }
        int Runtime { get; }
        object[] Seasons { get; }
        string SeriesType { get; }
        string SortTitle { get; }
        object Statistics { get; }
        string Status { get; }
        string Title { get; }
        string TitleSlug { get; }
        int TVDbId { get; }
        int TvMazeId { get; }
        int TvRageId { get; }
        bool UseSceneNumbering { get; }
        bool UseSeasonFolders { get; }
        int Year { get; }
    }
}
