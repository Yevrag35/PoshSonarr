using MG.Sonarr.Next.Extensions.PSO;
using System.Management.Automation;

namespace MG.Sonarr.Next.Models.Releases
{
    public readonly struct Weight
    {
        static readonly string[] _weightProps = new string[]
        {
            "LanguageWeight", "PreferredWordScore", "QualityWeight", "ReleaseWeight"
        };

        readonly int _langWeight;
        readonly int _releaseWeight;
        readonly int _qualWeight;
        readonly int _prefWord;
        readonly int _total;

        public int LanguageWeight => _langWeight;
        public int PreferredWordScore => _prefWord;
        public int ReleaseWeight => _releaseWeight;
        public int QualityWeight => _qualWeight;
        public int TotalWeight => _total;

        public Weight(ReleaseObject release)
        {
            int langWeight = GetValueOrDefault(release, _weightProps[0]);
            int prefWord = GetValueOrDefault(release, _weightProps[1]);
            int qualWeight = GetValueOrDefault(release, _weightProps[2]);
            int releaseWeight = GetValueOrDefault(release, _weightProps[3]);
            _total = langWeight + prefWord + qualWeight + releaseWeight;
            _langWeight = langWeight;
            _qualWeight = qualWeight;
            _prefWord = prefWord;
            _releaseWeight = releaseWeight;
        }

        private static int GetValueOrDefault(ReleaseObject release, string propertyName)
        {
            PSPropertyInfo? prop = release.Properties[propertyName];
            return prop?.Value switch
            {
                int i => i,
                string s => int.TryParse(s, out int isNum) ? isNum : 0,
                _ => 0,
            };
        }

        public void SetRelease(ReleaseObject release)
        {
            release.UpdateProperty(_weightProps[0], _langWeight);
            release.UpdateProperty(_weightProps[1], _prefWord);
            release.UpdateProperty(_weightProps[2], _qualWeight);
            release.UpdateProperty(_weightProps[3], _releaseWeight);
        }
    }
}
