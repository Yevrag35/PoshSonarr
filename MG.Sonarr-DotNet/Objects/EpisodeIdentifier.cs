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

        public HashSet<int> AbsoluteEpisodes { get; private set; } = new HashSet<int>();
        public HashSet<int> AbsoluteSeasons { get; private set; } = new HashSet<int>();
        public HashSet<(int, int)> SeasonEpisodePairs { get; private set; } = new HashSet<(int, int)>();

        /// <summary>
        /// The parsed episode number(s) to search for.
        /// </summary>
        [Obsolete]
        public HashSet<int> Episodes { get; private set; } = new HashSet<int>();

        /// <summary>
        /// Indicates that the episode numbers in <see cref="Episodes"/> are absolute and
        /// not in the context of any individual season.
        /// </summary>
        [Obsolete]
        public bool IsAbsoluteEpisodeNumber => this.Episodes.Count > 0 && this.Seasons.Count <= 0;

        /// <summary>
        /// The parsed season number(s) to search for.
        /// </summary>
        [Obsolete]
        public HashSet<int> Seasons { get; private set; } = new HashSet<int>();

        public EpisodeIdentifier()
        {
        }
        [Obsolete]
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

        private bool Contains(int? number, ISet<int> set) => set.Count > 0 && number.HasValue && set.Contains(number.Value);
        private bool ContainsOrIsEmpty(int? season, int? episode, ISet<(int, int)> set) => set.Count > 0
            &&
            (
                season.HasValue && episode.HasValue
                &&
                set.Contains((season.Value, episode.Value))
            );

        public bool FallsInRange(IEpisode episode)
        {
            return
                this.ContainsOrIsEmpty(episode.SeasonNumber, episode.EpisodeNumber, this.SeasonEpisodePairs)
                ||
                this.Contains(episode.AbsoluteEpisodeNumber, this.AbsoluteEpisodes)
                ||
                this.Contains(episode.SeasonNumber, this.AbsoluteSeasons);
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
            var identifier = new EpisodeIdentifier();
            var table = HelperTable.NewTable(objs);

            if (table.HasNoKey)
                IdFromStrings(table.GetUniqueNoKeyValues().OfType<string>(), ref identifier);
            

            if (table.Keys.Count > 0 && (table.Keys.Count > 1 && table.HasNoKey))
                IdsFromTable(table, ref identifier);

            return identifier;
        }

        private static void IdFromStrings(IEnumerable<string> strs, ref EpisodeIdentifier addTo)
        {
            foreach (string s in strs)
            {
                Match rgm = Regex.Match(s, REGEX, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                if (!rgm.Success)
                    throw new ArgumentException(string.Format(EX_MSG, s));

                bool isAbsoluteEp = rgm.Groups[GRP_SEASON].Length <= 0;
                bool isAbsoluteSeason = rgm.Groups[GRP_EPISODE].Length <= 0;
                int season = 0;
                int episode = 0;

                if (!isAbsoluteEp && int.TryParse(rgm.Groups[GRP_SEASON_NUMBER].Value, out int seasonNumber))
                {
                    season = seasonNumber;
                }
                else
                    isAbsoluteEp = true;

                if (rgm.Groups[GRP_EPISODE].Length > 1 && int.TryParse(rgm.Groups[GRP_EPISODE_NUMBER].Value, out int episodeNumber))
                {
                    episode = episodeNumber;
                }
                else
                    isAbsoluteSeason = true;

                if (!isAbsoluteSeason && !isAbsoluteEp)
                    addTo.SeasonEpisodePairs.Add((season, episode));

                else if (isAbsoluteSeason)
                    addTo.AbsoluteSeasons.Add(season);

                else if (isAbsoluteEp)
                    addTo.AbsoluteEpisodes.Add(episode);
            }
        }
        private static void IdsFromTable(HelperTable helper, ref EpisodeIdentifier identifier)
        {
            string seasonKey = null;
            string episodeKey = null;
            bool isAbsoluteSeason = true;
            bool isAbsoluteEpisode = true;

            if (helper.ContainsKey("season"))
            {
                seasonKey = "season";
                isAbsoluteEpisode = false;
            }
            else if (helper.ContainsKey("seasons"))
            {
                seasonKey = "seasons";
                isAbsoluteEpisode = false;
            }
            else
                isAbsoluteSeason = false;

            if (helper.ContainsKey("episode"))
            {
                episodeKey = "episode";
                isAbsoluteSeason = false;
            }
            else if (helper.ContainsKey("episodes"))
            {
                episodeKey = "episodes";
                isAbsoluteSeason = false;
            }
            else
                isAbsoluteEpisode = false;

            if (isAbsoluteSeason)
                identifier.GetIntsFromObjs(helper[seasonKey], x => x.AbsoluteSeasons);

            else if (isAbsoluteEpisode)
                identifier.GetIntsFromObjs(helper[episodeKey], x => x.AbsoluteEpisodes);

            else if (!string.IsNullOrEmpty(seasonKey) && !string.IsNullOrEmpty(episodeKey))
            {
                IEnumerable<(int, int)> pairs = FormPairs(helper[seasonKey], helper[episodeKey]);
                identifier.SeasonEpisodePairs.UnionWith(pairs);
            }


            if (helper.ContainsKey("absolute"))
            {
                identifier.GetIntsFromObjs(helper["absolute"], x => x.AbsoluteEpisodes);
            }
            else if (helper.ContainsKey("absoluteepisodes"))
            {
                identifier.GetIntsFromObjs(helper["absoluteepisodes"], x => x.AbsoluteEpisodes);
            }
        }
        private static IEnumerable<(int, int)> FormPairs(IEnumerable<object> seasons, IEnumerable<object> episodes)
        {
            IEnumerable<int> seasonCol = seasons.Cast<int>().Distinct();
            IEnumerable<int> episodeCol = episodes.Cast<int>().Distinct();
            foreach (int season in seasonCol)
            {
                foreach (int ep in episodeCol)
                {
                    yield return (season, ep);
                }
            }
        }

        /// <summary>
        /// An implicit cast operator converting a <see cref="string"/> to an <see cref="EpisodeIdentifier"/>.
        /// </summary>
        /// <param name="str">The <see cref="string"/> which will be parsed by Regex to discern season and episode numbers.</param>
        //[Obsolete]
        //public static implicit operator EpisodeIdentifier(string str) => new EpisodeIdentifier(str);
    }
}
