using MG.Sonarr.Next.Extensions.PSO;
using System.Collections.Immutable;
using System.Management.Automation;

namespace MG.Sonarr.Next.Models.Releases
{
    [StructLayout(LayoutKind.Auto)]
    public readonly struct Weight
    {
        static readonly ImmutableArray<string> _weightProps =
        [
            "LanguageWeight", "PreferredWordScore", "QualityWeight", "ReleaseWeight"
        ];

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
            release.UpdateProperty(_langWeight, propertyName: _weightProps[0]);
            release.UpdateProperty(_prefWord, propertyName: _weightProps[1]);
            release.UpdateProperty(_qualWeight, propertyName: _weightProps[2]);
            release.UpdateProperty(_releaseWeight, propertyName: _weightProps[3]);
        }
    }
}
