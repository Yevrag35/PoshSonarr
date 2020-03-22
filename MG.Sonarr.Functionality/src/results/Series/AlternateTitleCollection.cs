using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace MG.Sonarr.Results
{
    public class AlternateTitleCollection : ResultCollectionBase<AlternateTitle>
    {
        public AlternateTitleCollection() : base() { }
        internal AlternateTitleCollection(int capacity) : base(capacity) { }
        [JsonConstructor]
        internal AlternateTitleCollection(IEnumerable<AlternateTitle> ats) : base(ats) { }

        #region METHODS
        public bool AnyHasSeasonNumber() => base.Contains(at => at.SeasonNumber.HasValue);
        public bool Contains(string title, bool isCaseSensitive = false)
        {
            StringComparison comparison = isCaseSensitive
                ? StringComparison.CurrentCulture
                : StringComparison.CurrentCultureIgnoreCase;

            return base.Contains(at => at.Title.Equals(title, comparison));
        }
        public bool HasTitle(string wildcardTitle, bool isCaseSensitive = false)
        {
            string pattern = base.WildcardStringToRegex(wildcardTitle);
            return base.Contains(at => Regex.IsMatch(at.Title, pattern, isCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase));
        }
        public IEnumerable<string> GetTitleFromSeasonNumber(params int[] seasonNumbers)
        {
            if (seasonNumbers == null || seasonNumbers.Length <= 0)
                return null;

            return base.FindAll(at => at.SeasonNumber.HasValue && seasonNumbers.Contains(at.SeasonNumber.Value))
                .Select(t => t.Title);
        }

        #endregion
    }
}