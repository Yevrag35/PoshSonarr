namespace MG.Sonarr.Next.Models.Episodes
{
    public interface IEpisodeIdentifier
    {
        int Episode { get; }
        bool IsAbsolute { get; }
        int Season { get; }

        bool IsValid();
    }
}

