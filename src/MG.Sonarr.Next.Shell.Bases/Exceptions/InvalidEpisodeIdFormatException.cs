using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Models.Episodes;
using System.Runtime.Serialization;

namespace MG.Sonarr.Next.Shell.Exceptions
{
    public sealed class InvalidEpisodeIdFormatException : PoshSonarrException
    {
        const string ARG_MSG = "Episode identifiers should be either be an integer or a string in \"S<season#>E<episode#>\" format.";

        public IReadOnlyList<IEpisodeIdentifier> OffendingIds { get; }

        public InvalidEpisodeIdFormatException(IReadOnlyList<IEpisodeIdentifier>? identifiers, Exception? innerException)
            : base(ARG_MSG, innerException)
        {
            this.OffendingIds = identifiers ?? Array.Empty<IEpisodeIdentifier>();
        }

        public static InvalidEpisodeIdFormatException FromList<T>(IReadOnlyList<T>? identifiers, Exception? innerException)
            where T : IEpisodeIdentifier
        {
            if (identifiers is null || identifiers.Count <= 0)
            {
                return new InvalidEpisodeIdFormatException(null, innerException);
            }

            var array = new IEpisodeIdentifier[identifiers.Count];
            for (int i = 0; i < identifiers.Count; i++)
            {
                array[i] = new StoredId(identifiers[i]);
            }

            return new InvalidEpisodeIdFormatException(array, innerException);
        }

        private readonly struct StoredId : IEpisodeIdentifier
        {
            readonly EpisodeRange _ep;
            readonly int _season;
            readonly bool _isAb;
            readonly bool _isNotEmpty;

            public EpisodeRange EpisodeRange => _ep;
            public bool IsAbsolute => _isAb;
            internal bool IsEmpty => !_isNotEmpty;
            public int Season => _season;

            internal StoredId(IEpisodeIdentifier other)
            {
                _ep = other.EpisodeRange;
                _season = other.Season;
                _isNotEmpty = other.IsValid();
                _isAb = other.IsAbsolute;
            }

            public bool IsValid()
            {
                return _isNotEmpty;
            }
        }
    }
}