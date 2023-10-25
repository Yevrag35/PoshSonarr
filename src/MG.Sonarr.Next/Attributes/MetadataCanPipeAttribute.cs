using MG.Sonarr.Next.Metadata;
using System.Management.Automation;

namespace MG.Sonarr.Next.Attributes
{
    /// <summary>
    /// An attribute for a Sonarr <see cref="PSCmdlet"/> class indicating the specified <see cref="MetadataTag"/>
    /// should have the decorated cmdlet's name to the tag's <see cref="MetadataTag.CanPipeTo"/> set.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class MetadataCanPipeAttribute : Attribute
    {
        /// <summary>
        /// The <see cref="MetadataTag.Value"/> of the tag that the name formed from the <see cref="CmdletAttribute"/>
        /// will be added to the tag's <see cref="MetadataTag.CanPipeTo"/> set.
        /// </summary>
        public required string Tag { get; init; }
    }
}
