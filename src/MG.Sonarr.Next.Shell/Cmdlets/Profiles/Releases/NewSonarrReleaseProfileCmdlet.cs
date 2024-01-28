using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using System.Collections;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles.Releases
{
    [Cmdlet(VerbsCommon.New, "SonarrReleaseProfile", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    public sealed class NewSonarrReleaseProfileCmdlet : SonarrMetadataCmdlet
    {
        protected override bool CaptureDebugPreference => true;

        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        [ValidateLength(1, int.MaxValue)]
        public string Name { get; set; } = string.Empty;

        [Parameter]
        public SwitchParameter Enabled { get; set; }

        [Parameter]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int IndexerId { get; set; }

        [Parameter]
        public SwitchParameter IncludePreferredWhenRenaming { get; set; }

        [Parameter]
        [ValidateNotNull]
        [ValidateLength(1, int.MaxValue)]
        [Alias("Ignored")]
        public string[] IgnoredTerms { get; set; } = Array.Empty<string>();

        [Parameter]
        [ValidateNotNull]
        [Alias("Preferred")]
        [ValidateDictionary(typeof(string), typeof(int))]
        public IDictionary PreferredTerms { get; set; } = null!;

        [Parameter]
        [ValidateNotNull]
        [ValidateLength(1, int.MaxValue)]
        [Alias("Required")]
        public string[] RequiredTerms { get; set; } = Array.Empty<string>();

        [Parameter]
        [ValidateNotNull]
        public int[] Tags { get; set; } = Array.Empty<int>();


        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.RELEASE_PROFILE];
        }

        protected override void Begin(IServiceProvider provider)
        {
            //var profile = provider.GetRequiredService<ReleaseProfileObject>();
            //profile.Name = this.Name;
        }

        protected override void Process(IServiceProvider provider)
        {
            //var profile = provider.GetRequiredService<ReleaseProfileObject>();
            //profile.Tags.UnionWith(this.Tags);
            //profile.Properties.Add(new PSNoteProperty(nameof(this.Tags), profile.Tags));

            //profile.Properties.Add(new PSNoteProperty(nameof(this.Enabled), this.Enabled.ToBool()));
            //profile.Properties.Add(new PSNoteProperty(nameof(this.IndexerId), this.IndexerId));
            //profile.Properties.Add(new PSNoteProperty(nameof(this.IncludePreferredWhenRenaming), this.IncludePreferredWhenRenaming.ToBool()));
            //profile.Properties.Add(new PSNoteProperty("Required", this.RequiredTerms));
            //profile.Properties.Add(new PSNoteProperty("Ignored", this.IgnoredTerms));
            //AddPreferredTerms(this.PreferredTerms, profile);

            //profile.Properties.Remove(Constants.ID);

            var body = new
            {
                this.Name,
                Enabled = this.Enabled.ToBool(),
                this.Tags,
                IncludePreferredWhenRenaming = this.IncludePreferredWhenRenaming.ToBool(),
                this.IndexerId,
                Required = this.RequiredTerms,
                Ignored = this.IgnoredTerms,
                Preferred = GetPreferredTerms(this.PreferredTerms).ToArray(),
            };

            this.SerializeIfDebug(body, includeType: false);

            if (this.ShouldProcess(this.Tag.UrlBase, "Create New Release Profile"))
            {
                this.CreateProfile(body);
            }
        }

        private void CreateProfile<T>(T body) where T : notnull
        {
            var response = this.SendPostRequest<T, ReleaseProfileObject>(this.Tag.UrlBase, body);
            if (response.TryPickT0(out var created, out var error))
            {
                this.WriteObject(created);
            }
            else
            {
                this.WriteError(error);
            }
        }

        private static IEnumerable<KeyValuePair<string, int>> GetPreferredTerms(IDictionary table)
        {
            if (table.Count <= 0)
            {
                return Enumerable.Empty<KeyValuePair<string, int>>();
            }

            var pairs = new KeyValuePair<string, int>[table.Count];

            int i = 0;
            foreach (KeyValuePair<IConvertible, int> kvp in table.EnumerateAsPairs<IConvertible, int>())
            {
                pairs[i++] = new KeyValuePair<string, int>(Convert.ToString(kvp.Key) ?? string.Empty, kvp.Value);
            }

            return pairs;
        }

        //private static bool IsStringKey(DictionaryEntry dictionaryEntry, [NotNullWhen(true)] out string? key)
        //{
        //    if (dictionaryEntry.Key is string strKey)
        //    {
        //        key = strKey;
        //        return !string.IsNullOrWhiteSpace(key);
        //    }
        //    else
        //    {
        //        key = null;
        //        return false;
        //    }
        //}

        //private static bool IsProperDictionaryEntry(object obj, out KeyValuePair<string, int> result)
        //{
        //    if (obj is DictionaryEntry de && de.Key is string strKey && de.Value is int intVal)
        //    {
        //        result = new(strKey, intVal);
        //        return true;
        //    }
        //    else
        //    {
        //        result = default;
        //        return false;
        //    }
        //}
    }
}

