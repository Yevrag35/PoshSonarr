using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Tags;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Shell.Attributes;
using MG.Sonarr.Next.Shell.Components;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags
{
    [Cmdlet(VerbsCommon.Rename, "SonarrTag", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true,
        DefaultParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
    [MetadataCanPipe(Tag = Meta.TAG)]
    public sealed class RenameSonarrTagCmdlet : SonarrApiCmdletBase
    {
        OneOf<string, ScriptBlock> _oneOf;
        TagObject? _pipedObject;
        MetadataTag _tag = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int Id { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSET_PIPELINE)]
        [ValidateId(ValidateRangeKind.Positive)]
        public TagObject? InputObject
        {
            get => _pipedObject;
            set
            {
                if (value is not null)
                {
                    _pipedObject = value;
                    this.Id = value.Id;
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSET_PIPELINE)]
        [Parameter(Mandatory = true, Position = 1, ParameterSetName = PSConstants.PSET_EXPLICIT_ID)]
        [ValidateNotNullOrEmpty]
        public string NewName { get; set; } = string.Empty;

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _tag = provider.GetRequiredService<IMetadataResolver>()[Meta.TAG];
        }

        protected override void Begin(IServiceProvider provider)
        {
            _oneOf = this.TryNameToScriptBlock(this.NewName, out ScriptBlock? sb) ? sb : this.NewName;
        }
        protected override void Process(IServiceProvider provider)
        {
            string ordinaryName = this.NewName;

            TagRename rename;
            if (this.InputObject is not null && _oneOf.TryPickT1(out ScriptBlock? newNameBlock, out ordinaryName))
            {
                if (!this.TryGenerateNewName(newNameBlock, this.InputObject, out string? newName))
                {
                    this.WriteWarning("Cannot rename a tag with a null or empty name.");
                    return;
                }

                rename = TagRename.Create(this.InputObject.Id, newName);
            }
            else
            {
                rename = TagRename.Create(this.Id, ordinaryName);
            }

            string url = _tag.GetUrlForId(rename.Id);

            if (this.ShouldProcess(url, $"Renaming Tag -> {rename.Label}"))
            {
                SonarrResponse response = this.SendPutRequest(url, rename);

                if (response.IsError)
                {
                    this.WriteError(response.Error);
                    return;
                }

                this.WriteVerbose($"Renamed Tag -> {this.Id}");
            }
        }

        private bool TryNameToScriptBlock(string newName, [NotNullWhen(true)] out ScriptBlock? scriptBlock)
        {
            try
            {
                scriptBlock = this.SessionState.InvokeCommand.NewScriptBlock(newName);
                return scriptBlock.IsProperScriptBlock();
            }
            catch (Exception e)
            {
                scriptBlock = null;
                this.WriteError(e.ToRecord(ErrorCategory.ParserError, newName));
                return false;
            }
        }
        private bool TryGenerateNewName(ScriptBlock scriptBlock, TagObject tag, [NotNullWhen(true)] out string? newLabel)
        {
            ArgumentNullException.ThrowIfNull(tag);
            newLabel = null;

            try
            {
                newLabel = scriptBlock.InvokeWith<TagObject, string>(tag, this.ErrorPreference);
                return !string.IsNullOrWhiteSpace(newLabel);
            }
            catch (Exception e)
            {
                this.WriteError(e.ToRecord(ErrorCategory.InvalidOperation, scriptBlock));
                return false;
            }
        }
    }
}
