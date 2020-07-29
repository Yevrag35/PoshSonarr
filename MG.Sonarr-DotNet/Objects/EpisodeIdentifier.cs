using MG.Sonarr.Functionality.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MG.Sonarr
{
    /// <summary>
    /// A class that converts a season-episode <see cref="string"/> representation to a logic-based one.
    /// </summary>
    public class EpisodeIdentifier
    {
        private const string EX_MSG = "\"{0}\" is not a valid EpisodeIdentifier.  Proper identifier examples include: \"s01e18\", \"S7E1\", \"s12\"";
        private const string REGEX = @"^s(\d{1,2})(?:e(\d{1,2})|)$";

        /// <summary>
        /// The parsed episode number from the forming <see cref="string"/>.  
        /// This property can be null.
        /// </summary>
        public HashSet<int> Episodes { get; private set; } = new HashSet<int>();
        //public int? Episode { get; private set; }
        /// <summary>
        /// The parsed season number from the forming <see cref="string"/>.
        /// </summary>
        public HashSet<int> Season { get; private set; } = new HashSet<int>();
        //public int Season { get; private set; }

        private EpisodeIdentifier(string str)
        {
            Match rgm = Regex.Match(str, REGEX, RegexOptions.IgnoreCase);
            if (!rgm.Success)
                throw new ArgumentException(string.Format(EX_MSG, str));

            this.Season.Add(int.Parse(rgm.Groups[1].Value));
            if (rgm.Groups.Count == 3 && rgm.Groups.Cast<Group>().All(x => x.Success))
            {
                this.Episodes.Add(int.Parse(rgm.Groups[2].Value));
            }
        }
        private EpisodeIdentifier(HelperTable helper)
        {
            if (helper.ContainsKey("episode"))
            {
                int[] epInts = this.GetIntsFromObjs(helper["episode"]);
                this.Episodes.UnionWith(epInts);
            }
            else if (helper.ContainsKey("episodes"))
            {
                int[] epsInts = this.GetIntsFromObjs(helper["episodes"]);
                this.Episodes.UnionWith(epsInts);
            }

            if (helper.ContainsKey("season"))
            {
                int[] season = this.GetIntsFromObjs(helper["season"]);
                this.Season.UnionWith(season);
            }
            else if (helper.ContainsKey("seasons"))
            {
                int[] seasons = this.GetIntsFromObjs(helper["seasons"]);
                this.Season.UnionWith(seasons);
            }
        }

        private int[] GetIntsFromObjs(List<object> list)
        {
            int[] ints = new int[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                ints[i] = Convert.ToInt32(list[i]);
            }
            return ints;
        }

        public static IEnumerable<EpisodeIdentifier> GetEpisodeIdentifiers(object[] objs)
        {
            var table = HelperTable.NewTable(objs);
            if (table.ContainsKey("nokey"))
            {
                foreach (string str in table["nokey"])
                {
                    yield return new EpisodeIdentifier(str);
                }
            }
            else
                yield return new EpisodeIdentifier(table);
        }

        /// <summary>
        /// An implicit cast operator converting a <see cref="string"/> to an <see cref="EpisodeIdentifier"/>.
        /// </summary>
        /// <param name="str">The <see cref="string"/> which will be parsed by Regex to discern season and episode numbers.</param>
        public static implicit operator EpisodeIdentifier(string str) => new EpisodeIdentifier(str);
        //public static implicit operator EpisodeIdentifier(object o)
        //{
        //    //HelperTable table = HelperTable.NewTable(new object[1] { hashtable });
        //    HelperTable table = HelperTable.NewTable(new object[1] { o });
        //    return new EpisodeIdentifier(table);
        //}

        //public static implicit operator EpisodeIdentifier(Hashtable hashtable)
    }
}
