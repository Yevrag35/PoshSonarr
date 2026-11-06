using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Commands;
using MG.Sonarr.Next.Services.Jobs;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using MG.Sonarr.Next.Shell.Output;

namespace MG.Sonarr.Next.Shell.Cmdlets.Commands;

/// <summary>
/// Represents the <c>Get-SonarrCommand</c> cmdlet, which gets the current state of commands that Sonarr has queued or
/// run.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet takes the commands by <c>-Id</c>, or as <see cref="ICommand"/> values piped to <c>-InputObject</c>, such
/// as the <see cref="CommandObject"/> that <c>Invoke-SonarrRssSync</c> writes, and collects their IDs until the
/// pipeline ends. It then gets each command with a request of its own and writes it to the pipeline as a
/// <see cref="CommandObject"/>, in order of ID. An ID that Sonarr doesn't have writes no error.
/// </para>
/// <para>
/// A command that the session's command history already holds is replaced there with the state that Sonarr returned.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrCommand", DefaultParameterSetName = PSConstants.PSetExplicitId)]
[MetadataCanPipe(Tag = Meta.Command)]
[OutputType(typeof(ICommandOutput))]
public sealed class GetSonarrCommandCmdlet : SonarrMetadataCmdlet
{
	private SortedSet<int> _ids = null!;

	/// <summary>
	/// Gets or sets the commands to get again.
	/// </summary>
	/// <remarks>
	/// The parameter takes pipeline input, and the cmdlet reads only each command's ID.
	/// </remarks>
	/// <value>The commands. The default is an empty array.</value>
	[Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = PSConstants.PSetPipeline)]
	public ICommand[] InputObject { get; set; } = Array.Empty<ICommand>();

	/// <summary>
	/// Gets or sets the IDs of the commands to get.
	/// </summary>
	/// <remarks>
	/// Each ID has to be greater than zero.
	/// </remarks>
	/// <value>The command IDs. The default is an empty array.</value>
	[Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSetExplicitId)]
	[ValidateRange(ValidateRangeKind.Positive)]
	public int[] Id { get; set; } = [];

	/// <summary>
	/// Gets the metadata tag of the command endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The command endpoint's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.Command];
	}
	/// <summary>
	/// Runs the base class's setup for the scope, then creates the set of command IDs that the cmdlet fills.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_ids = [];
	}

	/// <summary>
	/// Adds the IDs from <c>-Id</c> to the IDs to get.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id ?? Array.Empty<int>());
	}
	/// <summary>
	/// Adds the IDs of the piped commands to the IDs to get.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	[SuppressMessage("Style", "IDE0009:Member access should be qualified.", Justification = "Used in implicit naming.")]
	protected override void Process(IServiceProvider provider)
	{
		if (this.HasParameter(InputObject) && this.InputObject.Length > 0)
		{
			foreach (ICommand cmd in this.InputObject)
			{
				_ids.Add(cmd.Id);
			}
		}
	}
	/// <summary>
	/// Gets each collected command from Sonarr, writes it to the pipeline, and replaces the command history's entry for
	/// it, when the history has one.
	/// </summary>
	/// <remarks>
	/// When a request fails, the cmdlet writes its error, unless the error is ignorable, as the one for an ID that
	/// doesn't exist is, and goes on to the next ID.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void End(IServiceProvider provider)
	{
		ICommandHistory history = provider.GetRequiredService<ICommandHistory>();

		foreach (int id in _ids)
		{
			string url = this.Tag.GetUrlForId(id);
			var response = this.SendGetRequest<CommandObject>(url);

			if (this.TryWriteObject(response) && history.Remove(id))
			{
				history.Add(response.Value);
			}
		}
	}
}
