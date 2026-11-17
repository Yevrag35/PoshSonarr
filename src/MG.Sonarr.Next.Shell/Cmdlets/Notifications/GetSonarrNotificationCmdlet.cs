using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Notifications;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;

namespace MG.Sonarr.Next.Shell.Cmdlets.Notifications;

/// <summary>
/// Represents the <c>Get-SonarrNotification</c> cmdlet, which gets the notification connections that Sonarr has.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet writes each connection to the pipeline as a <see cref="NotificationObject"/>. With no <c>-Id</c> or
/// <c>-Name</c>, it writes every connection.
/// </para>
/// <para>
/// <c>-Id</c> gets the connections with those IDs, with one request for each ID, and <c>-Name</c> gets every connection
/// and keeps those whose name matches one of the names. Given together, the cmdlet writes the connections with the IDs
/// first, and then those whose name matches, leaving out the ones that it already wrote.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Get, "SonarrNotification", DefaultParameterSetName = "None")]
public sealed class GetSonarrNotificationCmdlet : SonarrMetadataCmdlet
{
	private SortedSet<int> _ids = null!;
	private WildcardSet _wcNames = null!;

	/// <summary>
	/// Gets or sets the IDs of the notification connections to get.
	/// </summary>
	/// <remarks>
	/// The cmdlet sends one request for each ID, and writes nothing for an ID that Sonarr has no connection for.
	/// </remarks>
	/// <value>The IDs of the connections. The default is an empty array.</value>
	[Parameter(Mandatory = true, Position = 0, ParameterSetName = PSConstants.PSetExplicitId)]
	public int[] Id { get; set; } = Array.Empty<int>();

	/// <summary>
	/// Gets or sets the names of the notification connections to get.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A name can hold wildcards and matches a connection's name ignoring case.
	/// </para>
	/// <para>
	/// The parameter comes first by position, except after <c>-Id</c>, where it comes second, so
	/// <c>Get-SonarrNotification 5 Email*</c> gives both.
	/// </para>
	/// </remarks>
	/// <value>The names of the connections. The default is an empty array.</value>
	[Parameter(Position = 0, ParameterSetName = "None")]
	[Parameter(Position = 1, ParameterSetName = PSConstants.PSetExplicitId)]
	public string[] Name { get; set; } = Array.Empty<string>();

	/// <summary>
	/// Gets the metadata tag of the notification endpoint.
	/// </summary>
	/// <param name="resolver">The resolver of the cmdlet's scope.</param>
	/// <returns>The notification's metadata tag.</returns>
	protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
	{
		return resolver[Meta.Notification];
	}
	/// <summary>
	/// Runs the base class's setup for the scope, then creates the sets of IDs and names that the cmdlet fills.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void OnCreatingScope(IServiceProvider provider)
	{
		base.OnCreatingScope(provider);
		_ids = [];
		_wcNames = new();
	}

	/// <summary>
	/// Adds the values of <c>-Id</c> and <c>-Name</c> to the cmdlet's sets of IDs and names.
	/// </summary>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Begin(IServiceProvider provider)
	{
		_ids.UnionWith(this.Id);
		if (this.HasParameter(this.Name))
		{
			_wcNames.UnionWith(this.Name);
		}
	}

	/// <summary>
	/// Writes to the pipeline the notification connections with the IDs, and then those whose name matches one of the
	/// names, or every connection when there are neither IDs nor names.
	/// </summary>
	/// <remarks>
	/// A request for an ID that fails writes its error, unless the error is ignorable, as the one for an ID that doesn't
	/// exist is. When the request for every connection fails, the cmdlet stops with its error.
	/// </remarks>
	/// <param name="provider">The cmdlet's scoped service provider.</param>
	protected override void Process(IServiceProvider provider)
	{
		bool gotIds = false;
		if (_ids.Count > 0)
		{
			gotIds = true;
			var fromIds = this.GetById<NotificationObject>(_ids);
			this.WriteCollection(fromIds);
		}

		if (_wcNames.Count > 0 || !gotIds)
		{
			var fromNames = this.GetByName(_wcNames, _ids);
			this.WriteCollection(fromNames);
		}
	}

	/// <summary>
	/// Gets every notification connection and, when there are names, removes from the list those that have one of the
	/// specified IDs or a name that matches none of the names.
	/// </summary>
	/// <remarks>
	/// The connections with the IDs are removed because the cmdlet has already written them. With no names, the list
	/// keeps every connection. When the request fails, the cmdlet stops with its error, and the list is empty.
	/// </remarks>
	/// <param name="names">The names, which can hold wildcards, that the connections' names are matched against.</param>
	/// <param name="ids">The IDs of the connections that the cmdlet has already written.</param>
	/// <returns>The connections that match, or every connection when there are no names.</returns>
	private MetadataList<NotificationObject> GetByName(WildcardSet names, SortedSet<int> ids)
	{
		var response = this.GetAll<NotificationObject>();
		if (response.Count <= 0 || names.Count <= 0)
		{
			return response;
		}

		for (int i = response.Count - 1; i >= 0; i--)
		{
			var item = response[i];
			if (ids.Contains(item.Id) || !names.IsAnyMatch(item.Name))
			{
				response.RemoveAt(i);
			}
		}

		return response;
	}
}
