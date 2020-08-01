using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

namespace MG.Sonarr
{
    /// <summary>
    /// A class that converts a season-episode <see cref="string"/> or <see cref="IDictionary"/> representation to a logic-based one.
    /// </summary>
    public class EpisodeIdentifier
    {
        private const string EX_MSG = "\"{0}\" is not a valid EpisodeIdentifier.  Proper identifier examples include: \"s01e18\", \"S7E1\", \"s12\", \"E474\"";
        //private const string REGEX = @"^s(\d{1,2})(?:e(\d{1,2})|)$";
        private const string REGEX = @"^(?'season's(?'seasonNumber'\d{1,10})|)(?'episode'e(?'episodeNumber'\d{1,10})|)$";
        private const string GRP_SEASON = "season";
        private const string GRP_SEASON_NUMBER = "seasonNumber";
        private const string GRP_EPISODE = "episode";
        private const string GRP_EPISODE_NUMBER = "episodeNumber";

        /// <summary>
        /// The parsed episode number(s) to search for.
        /// </summary>
        public HashSet<int> Episodes { get; private set; } = new HashSet<int>();

        /// <summary>
        /// Indicates that the episode numbers in <see cref="Episodes"/> are absolute and
        /// not in the context of any individual season.
        /// </summary>
        public bool IsAbsoluteEpisodeNumber => this.Episodes.Count > 0 && this.Seasons.Count <= 0;

        /// <summary>
        /// The parsed season number(s) to search for.
        /// </summary>
        public HashSet<int> Seasons { get; private set; } = new HashSet<int>();

        public EpisodeIdentifier()
        {
        }
        private EpisodeIdentifier(HelperTable helper)
        {
            if (helper.ContainsKey("episode"))
            {
                this.GetIntsFromObjs(helper["episode"], x => x.Episodes);
            }
            else if (helper.ContainsKey("episodes"))
            {
                this.GetIntsFromObjs(helper["episodes"], x => x.Episodes);
            }

            if (helper.ContainsKey("season"))
            {
                this.GetIntsFromObjs(helper["season"], x => x.Seasons);
            }
            else if (helper.ContainsKey("seasons"))
            {
                this.GetIntsFromObjs(helper["seasons"], x => x.Seasons);
            }
        }

        private bool ContainsOrIsEmpty(int? number, ISet<int> set) => set.Count <= 0 || (number.HasValue && set.Contains(number.Value));
        public bool FallsInRange(IEpisode episode)
        {
            if (!this.IsAbsoluteEpisodeNumber)
            {
                return 
                    this.ContainsOrIsEmpty(episode.EpisodeNumber, this.Episodes) 
                    && 
                    this.ContainsOrIsEmpty(episode.SeasonNumber, this.Seasons);
            }
            else
            {
                return 
                    episode.AbsoluteEpisodeNumber.HasValue
                    &&
                    this.Episodes.Contains(episode.AbsoluteEpisodeNumber.Value);
            }
        }

        private void GetIntsFromObjs(List<object> list, Expression<Func<EpisodeIdentifier, HashSet<int>>> expression)
        {
            Func<EpisodeIdentifier, HashSet<int>> func = expression.Compile();
            HashSet<int> set = func(this);

            for (int i = 0; i < list.Count; i++)
            {
                set.Add(Convert.ToInt32(list[i]));
            }
        }

        /// <summary>
        /// Parses an instance of <see cref="EpisodeIdentifier"/> from the specified objects.
        /// </summary>
        /// <param name="objs">The objects to parse <see cref="EpisodeIdentifier"/>s from.</param>
        /// <returns>A instance of <see cref="EpisodeIdentifier"/>.</returns>
        public static EpisodeIdentifier Parse(object[] objs)
        {
            var table = HelperTable.NewTable(objs);

            if (table.HasNoKey)
            {
                return IdFromStrings(table.GetUniqueNoKeyValues().OfType<string>());
            }
            else
                return new EpisodeIdentifier(table);
        }

        private static EpisodeIdentifier IdFromStrings(IEnumerable<string> strs)
        {
            var identifier = new EpisodeIdentifier();
            foreach (string s in strs)
            {
                Match rgm = Regex.Match(s, REGEX, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                if (!rgm.Success)
                    throw new ArgumentException(string.Format(EX_MSG, s));

                if (rgm.Groups[GRP_SEASON].Length > 1 && int.TryParse(rgm.Groups[GRP_SEASON_NUMBER].Value, out int seasonNumber))
                    identifier.Seasons.Add(seasonNumber);

                if (rgm.Groups[GRP_EPISODE].Length > 1 && int.TryParse(rgm.Groups[GRP_EPISODE_NUMBER].Value, out int episodeNumber))
                    identifier.Episodes.Add(episodeNumber);
            }
            return identifier;
        }

        /// <summary>
        /// An implicit cast operator converting a <see cref="string"/> to an <see cref="EpisodeIdentifier"/>.
        /// </summary>
        /// <param name="str">The <see cref="string"/> which will be parsed by Regex to discern season and episode numbers.</param>
        //[Obsolete]
        //public static implicit operator EpisodeIdentifier(string str) => new EpisodeIdentifier(str);
    }
}
