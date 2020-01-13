using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Extensions
{
    internal static class QualityProfileExtensions
    {
        public static void AddAllowedQuality(this QualityProfile qualityProfile, string qualityName, bool caseSensitive = false, bool isAllowed = true)
        {
            if (Context.AllQualities == null || Context.AllQualities.Count <= 0)
                throw new SonarrContextNotSetException();

            StringComparison comparison = StringComparison.CurrentCultureIgnoreCase;
            if (caseSensitive)
                comparison = StringComparison.CurrentCulture;

            Quality matchingQuality = Context.AllQualities.Find(x => x.Name.Equals(qualityName, comparison));
            if (matchingQuality == null)
                throw new ArgumentException(string.Format("No matching quality named \"{0}\" was found.", qualityName));

            qualityProfile.AllowedQualities.AddFromQuality(matchingQuality, isAllowed);
        }
    }
}
