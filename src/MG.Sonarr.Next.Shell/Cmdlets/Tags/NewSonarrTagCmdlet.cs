using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Models.Tags;
using System.Text.Json;

namespace MG.Sonarr.Next.Shell.Cmdlets.Tags;

/// <summary>
/// Represents the <c>New-SonarrTag</c> cmdlet, which creates a tag in Sonarr.
/// </summary>
/// <remarks>
/// The cmdlet confirms the change, naming the JSON of the <see cref="SonarrTag"/> that it sends, posts it to Sonarr,
/// and writes the tag that Sonarr created to the pipeline as a <see cref="TagObject"/>. When the request fails, it
/// writes the error instead.
/// </remarks>
[Cmdlet(VerbsCommon.New, "SonarrTag", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
public sealed class NewSonarrTagCmdlet : SonarrApiCmdletBase
{
	/// <summary>
	/// Gets or sets the label of the new tag.
	/// </summary>
	/// <remarks>
	/// The label can't be <see langword="null"/> or empty. The parameter's alias is <c>-Name</c>.
	/// </remarks>
	/// <value>The tag's label.</value>
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	[Parameter(Mandatory = true, Position = 0)]
	[ValidateNotNullOrEmpty]
	[Alias("Name")]
	public string Label { get; set; } = null!;

	/// <summary>
	/// Creates the tag, once the change is confirmed, and writes it to the pipeline.
	/// </summary>
	/// <remarks>
	/// The confirmation names the tag's JSON, serialized with the connection's debugging options. When the request
	/// fails, the cmdlet writes its error, whether or not it's ignorable.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		SonarrTag tag = new() { Id = 0, Label = this.Label };

		string json = JsonSerializer.Serialize(
			value: tag,
			options: provider.GetService<ISonarrJsonOptions>()?.ForDebugging);

		if (this.ShouldProcess(json, "Creating Tag"))
		{
			var oneOf = this.SendPostRequest<TagObject>(Constants.Tag, tag);
			if (oneOf.TryGetT1(out TagObject? to, out var error))
			{
				this.WriteObject(to);
			}
			else
			{
				this.WriteError(error);
			}
		}
	}
}
