using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Indexers;
using MG.Sonarr.Next.Models.Profiles;
using MG.Sonarr.Next.Models.Tags;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Exceptions;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Unions;
using System;

namespace MG.Sonarr.Next.Shell.Cmdlets.Profiles.Delay
{
    [Cmdlet(VerbsCommon.New, "SonarrDelayProfile", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    public sealed class NewSonarrDelayProfileCmdlet : SonarrMetadataCmdlet
    {
        [Parameter(Mandatory = true), ValidateRange(1, int.MaxValue)]
        public int Order { get; set; }

        [Parameter(Mandatory = true)]
        public PreferredProtocol Protocol { get; set; }

        [Parameter]
        public TimeSpan TorrentDelay { get; set; }

        [Parameter]
        public TimeSpan UsenetDelay { get; set; }

        [Parameter]
        public SwitchParameter BypassIfHighestQuality { get; set; }

        [Parameter]
        public SwitchParameter BypassIfAboveCustomFormatScore { get; set; }

        [Parameter]
        public int MinimumCustomFormatScore { get; set; }

        [Parameter, ValidateNotNull, AllowEmptyCollection, ValidateIds(ValidateRangeKind.Positive, NullBehavior = InputNullBehavior.Ignore)]
        public Either<string, int>[] Tags { get; set; } = [];

        protected override int Capacity => 2;

        private SortedSet<int> _tagIds = null!;
        private WildcardSet _tagNames = null!;

        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.DELAY_PROFILE];
        }

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _tagIds = this.GetPooledObject<SortedSet<int>>();
            _tagNames = this.GetPooledObject<WildcardSet>();
            this.SetReturnables(_tagIds, _tagNames);
        }

        [SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in nameof()")]
        protected override void Begin(IServiceProvider provider)
        {
            if (this.HasParameter(this.Tags))
            {
                this.Tags.SplitToSets(_tagIds, _tagNames, explicitlyCalledForString: false);
            }

            if (_tagNames.Count > 0)
            {
                MetadataTag tag = provider.GetMetadataTag(Meta.TAG);
                this.ProcessNames(_tagNames, _tagIds, tag);
            }
        }

        protected override void Process(IServiceProvider provider)
        {
            var body = new
            {
                EnableUsenet = this.Protocol is PreferredProtocol.PreferUsenet or PreferredProtocol.OnlyUsenet,
                EnableTorrent = this.Protocol is PreferredProtocol.PreferTorrent or PreferredProtocol.OnlyTorrent,
                BypassIfHighestQuality = this.BypassIfHighestQuality.ToBool(),
                BypassIfAboveCustomFormatScore = this.BypassIfAboveCustomFormatScore.ToBool(),
                MinimumCustomFormatScore = this.MinimumCustomFormatScore,
                Tags = _tagIds,
                Order = this.Order,
                UsenetDelay = (int)this.UsenetDelay.TotalMinutes,
                TorrentDelay = (int)this.TorrentDelay.TotalMinutes,
                PreferredProtocol = this.Protocol is PreferredProtocol.OnlyTorrent or PreferredProtocol.PreferTorrent ? "torrent" : "usenet",
            };

            this.SerializeIfDebug(body, includeType: false);
            
            if (this.ShouldProcess(this.Tag.UrlBase, $"Create new delay profile with order -> {this.Order}"))
            {
                this.CreateProfile(body);
            }
        }

        private void CreateProfile<T>(T body) where T : notnull
        {
            var response = this.SendPostRequest<T, DelayProfileObject>(this.Tag.UrlBase, body);
            this.WriteOutcome(response);
        }

        private void ProcessNames(WildcardSet names, SortedSet<int> tagIds, MetadataTag tag)
        {
            if (names.Count == 0)
            {
                return;
            }

            var tags = this.GetAll<TagObject>(tag.UrlBase).AsSpan();

            for (int i = 0; i < tags.Length; i++)
            {
                TagObject tagObj = tags[i];
                if (!tagIds.Contains(tagObj.Id) && names.IsAnyMatch(tagObj.Label))
                {
                    tagIds.Add(tagObj.Id);
                }
            }
        }
    }

    public enum PreferredProtocol
    {
        PreferTorrent,
        PreferUsenet,
        OnlyTorrent,
        OnlyUsenet,
    };
}
