using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Services.Http;

namespace MG.Sonarr.Next.Shell.Cmdlets.Bases;

/// <summary>
/// Represents a <see cref="SonarrApiCmdletBase"/> that queries or changes one kind of Sonarr object, at the endpoint
/// that its metadata tag names.
/// </summary>
/// <remarks>
/// A derived cmdlet picks its tag in <see cref="GetMetadataTag(IMetadataResolver)"/>, and the get-all, filter, and
/// get-by-ID helpers send their requests to the tag's endpoint.
/// </remarks>
//[DebuggerStepThrough]
public abstract class SonarrMetadataCmdlet : SonarrApiCmdletBase
{
	MetadataTag? _tag;

	/// <summary>
	/// Gets the defined <see cref="MetadataTag"/> that this cmdlet deals with when querying
	/// Sonarr APIs.
	/// </summary>
	/// <remarks>
	/// The tag is set from <see cref="GetMetadataTag(IMetadataResolver)"/> when the cmdlet's scope is created, before
	/// <see cref="SonarrCmdletBase.OnCreatingScope(IServiceProvider)"/> runs.
	/// </remarks>
	/// <value>The cmdlet's metadata tag, or <see cref="MetadataTag.Empty"/> before the scope is created.</value>
	protected MetadataTag Tag
	{
		get => _tag ??= MetadataTag.Empty;
		private set => _tag = value;
	}

	/// <summary>
	/// Runs the base class's setup for a new scope and then sets <see cref="Tag"/> to the tag that
	/// <see cref="GetMetadataTag(IMetadataResolver)"/> returns for the scope's <see cref="IMetadataResolver"/>.
	/// </summary>
	/// <remarks>
	/// The override is sealed, so every derived cmdlet has its tag before
	/// <see cref="SonarrCmdletBase.OnCreatingScope(IServiceProvider)"/> runs.
	/// </remarks>
	/// <param name="provider">The scoped service provider of the cmdlet.</param>
	/// <exception cref="InvalidOperationException">
	/// Thrown when the scope has no <see cref="MG.Sonarr.Next.Services.Http.Clients.ISonarrClient"/> or no
	/// <see cref="IMetadataResolver"/>.
	/// </exception>
	private protected sealed override void OnCreatingScopeInternal(IServiceProvider provider)
	{
		base.OnCreatingScopeInternal(provider);
		this.Tag = this.GetMetadataTag(provider.GetRequiredService<IMetadataResolver>());
	}

	/// <summary>
	/// When implemented in a derived class, gets the metadata tag of the endpoint that the cmdlet works with.
	/// </summary>
	/// <remarks>
	/// The method runs once, when the cmdlet's scope is created, and its result becomes <see cref="Tag"/>.
	/// </remarks>
	/// <param name="resolver">The resolver of the scope, which holds the connection's metadata tags.</param>
	/// <returns>The cmdlet's metadata tag.</returns>
	protected abstract MetadataTag GetMetadataTag(IMetadataResolver resolver);
	/// <summary>
	/// Gets every object at the specified endpoint, or at the endpoint of <see cref="Tag"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The list is tagged, and sorted by <typeparamref name="T"/>'s own order when it holds more than one object.
	/// </para>
	/// <para>
	/// When the request fails, the method stops the cmdlet with the request's error, as
	/// <see cref="SonarrCmdletBase.StopCmdlet(ErrorRecord)"/> does, so no later input is processed, and returns an empty
	/// list. <c>-ErrorAction</c> decides what happens to the error.
	/// </para>
	/// </remarks>
	/// <typeparam name="T">The type of the objects at the endpoint.</typeparam>
	/// <param name="url">
	/// The endpoint's path, relative to the API, or <see langword="null"/> for the path of <see cref="Tag"/>.
	/// </param>
	/// <returns>The objects at the endpoint, or an empty list when the request fails.</returns>
	protected MetadataList<T> GetAll<T>(string? url = null) where T : PSObject, IComparable<T>, IJsonMetadataTaggable
	{
		var response = this.SendGetRequest<MetadataList<T>>(url ?? this.Tag.UrlBase);
		if (response.IsError)
		{
			this.StopCmdlet(response.Error);
			return [];
		}

		return response.Value;
	}
	/// <summary>
	/// Gets the objects at the specified endpoint, or at the endpoint of <see cref="Tag"/>, that have one of the
	/// specified IDs or a name that matches one of the specified wildcards.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The method gets every object, as <see cref="GetAll{T}(string?)"/> does, and removes from that list each object
	/// whose ID isn't in <paramref name="ids"/> and whose name matches none of <paramref name="names"/>, ignoring case.
	/// So an object is kept when it meets either condition, and when both sets are empty, no object is kept.
	/// </para>
	/// <para>
	/// When the request fails, the cmdlet stops with the request's error, as it does in
	/// <see cref="GetAll{T}(string?)"/>, and the list is empty.
	/// </para>
	/// </remarks>
	/// <typeparam name="T">The type of the objects at the endpoint.</typeparam>
	/// <param name="ids">The IDs of the objects to keep.</param>
	/// <param name="names">The wildcards that the names of the objects to keep match.</param>
	/// <param name="url">
	/// The endpoint's path, relative to the API, or <see langword="null"/> for the path of <see cref="Tag"/>.
	/// </param>
	/// <returns>The objects that match, in the order that <see cref="GetAll{T}(string?)"/> gives them.</returns>
	protected MetadataList<T> GetAllAndFilter<T>(SortedSet<int> ids, WildcardSet names, string? url = null) where T : PSObject, IComparable<T>, IHasId, IHasName, IJsonMetadataTaggable
	{
		MetadataList<T> list = this.GetAll<T>(url);
		if (list.Count == 0)
		{
			return list;
		}

		ReadOnlySpan<T> span = list.AsSpan();
		for (int i = span.Length - 1; i >= 0; i--)
		{
			T item = span[i];
			if (!ids.Contains(item.Id) && !names.IsAnyMatch(item.Name))
			{
				list.RemoveAt(i);
			}
		}

		return list;
	}
	/// <summary>
	/// Gets the objects with the specified IDs from the endpoint of <see cref="Tag"/>, with one request for each ID.
	/// </summary>
	/// <remarks>
	/// The requests go out in the order that <paramref name="ids"/> gives the IDs. When a request fails, the method
	/// writes its error, unless the error is ignorable, as the one for an ID that doesn't exist is, and goes on to the
	/// next ID.
	/// </remarks>
	/// <typeparam name="T">The type of the objects at the endpoint.</typeparam>
	/// <param name="ids">The IDs of the objects to get, or <see langword="null"/> for none.</param>
	/// <returns>
	/// The objects that were found, in the order that <paramref name="ids"/> gives their IDs, or an empty list when
	/// <paramref name="ids"/> is <see langword="null"/> or empty.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown when <paramref name="ids"/> isn't empty and the endpoint of <see cref="Tag"/> doesn't take an ID.
	/// </exception>
	protected List<T> GetById<T>(IReadOnlyCollection<int>? ids) where T : PSObject
	{
		if (ids.IsNullOrEmpty)
		{
			return [];
		}

		List<T> list = new(ids.Count);

		foreach (int id in ids)
		{
			string url = this.Tag.GetUrlForId(id);
			SonarrClientResult<T> response = this.SendGetRequest<T>(url);
			if (response.IsError)
			{
				this.WriteConditionalError(response.Error);
				continue;
			}

			list.Add(response.Value);
		}

		return list;
	}
}
