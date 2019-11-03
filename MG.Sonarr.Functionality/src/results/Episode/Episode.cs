﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// The class defining a response from the "/episode" endpoint.
    /// </summary>
    [Serializable]
    public class EpisodeResult : BaseEpisodeResult
    {
        public EpisodeFile EpisodeFile { get; set; }
        public int SeasonNumber { get; set; }
        public SeriesResult Series { get; set; }
    }

    public class EpisodeComparer : IComparer<EpisodeResult>
    {
        public int Compare(EpisodeResult x, EpisodeResult y)
        {
            if (x.AbsoluteEpisodeNumber.HasValue && !y.AbsoluteEpisodeNumber.HasValue)
                return 1;

            else if (!x.AbsoluteEpisodeNumber.HasValue && y.AbsoluteEpisodeNumber.HasValue)
                return -1;

            else if (!x.AbsoluteEpisodeNumber.HasValue && !y.AbsoluteEpisodeNumber.HasValue)
                return 0;

            else
            {
                return x.AbsoluteEpisodeNumber.Value.CompareTo(y.AbsoluteEpisodeNumber.Value);
            }
        }
    }

    public class EpisodeEquality : IEqualityComparer<EpisodeResult>
    {
        public bool Equals(EpisodeResult x, EpisodeResult y) => y.EpisodeId.Equals(y.EpisodeId);
        public int GetHashCode(EpisodeResult x) => x.GetHashCode();
    }
}