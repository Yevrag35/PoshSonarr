namespace MG.Sonarr.Next.Models.Episodes
{
    public interface IEpisodeIdentifier
    {
        EpisodeRange EpisodeRange { get; }
        bool IsAbsolute { get; }
        int Season { get; }

        bool IsValid();
    }
}

