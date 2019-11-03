using System;
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
        public int? Episode { get; private set; }
        /// <summary>
        /// The parsed season number from the forming <see cref="string"/>.
        /// </summary>
        public int Season { get; private set; }

        private EpisodeIdentifier(string str)
        {
            Match rgm = Regex.Match(str, REGEX, RegexOptions.IgnoreCase);
            if (!rgm.Success)
                throw new ArgumentException(string.Format(EX_MSG, str));

            this.Season = int.Parse(rgm.Groups[1].Value);
            if (rgm.Groups.Count == 3 && rgm.Groups.Cast<Group>().All(x => x.Success))
            {
                this.Episode = int.Parse(rgm.Groups[2].Value);
            }
        }

        /// <summary>
        /// An implicit cast operator converting a <see cref="string"/> to an <see cref="EpisodeIdentifier"/>.
        /// </summary>
        /// <param name="str">The <see cref="string"/> which will be parsed by Regex to discern season and episode numbers.</param>
        public static implicit operator EpisodeIdentifier(string str) => new EpisodeIdentifier(str);
    }
}
