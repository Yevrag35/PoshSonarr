using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Indexers;

/// <summary>
/// Represents an indexer that Sonarr searches for releases.
/// </summary>
/// <remarks>
/// The object carries the <see cref="Meta.INDEXER"/> tag, and pipes its ID as an <see cref="ITestPipeable"/>, so the
/// indexer can be tested. <see cref="Name"/> and <see cref="Protocol"/> are copies taken while the object is
/// deserialized and refreshed by a commit, and the other properties read their PowerShell properties.
/// </remarks>
[SonarrObject]
public sealed class IndexerObject : TagUpdateObject<IndexerObject>,
	IJsonOnSerializing,
	ISerializableNames<IndexerObject>,
	ITestPipeable
{
	/// <summary>
	/// The number of properties that an instance is sized for, about the number of fields that Sonarr sends.
	/// </summary>
	const int CAPACITY = 17;
	/// <summary>
	/// The full name of the class, which <see cref="SetPSTypeName"/> inserts into the object's type names.
	/// </summary>
	static readonly string _typeName = typeof(IndexerObject).GetName();

	/// <summary>
	/// Gets a value that indicates whether Sonarr reads the indexer's RSS feed.
	/// </summary>
	/// <value>The value of the <c>EnableRss</c> property, or <see langword="false"/> when the object has none.</value>
	public bool EnableRss
	{
		get => this.GetValue<bool>();
	}
	/// <summary>
	/// Gets a value that indicates whether Sonarr's automatic searches use the indexer.
	/// </summary>
	/// <value>
	/// The value of the <c>EnableAutomaticSearch</c> property, or <see langword="false"/> when the object has none.
	/// </value>
	public bool EnableAutomaticSearch
	{
		get => this.GetValue<bool>();
	}
	/// <summary>
	/// Gets a value that indicates whether interactive searches use the indexer.
	/// </summary>
	/// <value>
	/// The value of the <c>EnableInteractiveSearch</c> property, or <see langword="false"/> when the object has none.
	/// </value>
	public bool EnableInteractiveSearch
	{
		get => this.GetValue<bool>();
	}
	/// <summary>
	/// Gets the indexer's name.
	/// </summary>
	/// <remarks>
	/// The value is a copy of the <c>Name</c> property, so it doesn't follow a change that PowerShell makes until a
	/// request that sends the object succeeds.
	/// </remarks>
	/// <value>The indexer's name, or an empty string when it has none.</value>
	public string Name { get; private set; } = string.Empty;
	/// <summary>
	/// Gets or sets the indexer's priority.
	/// </summary>
	/// <remarks>
	/// <see cref="OnSerializing"/> sends 25 in place of a value outside 0 to 100.
	/// </remarks>
	/// <value>The value of the <c>Priority</c> property, or 0 when the object has none.</value>
	public int Priority
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}
	/// <summary>
	/// Gets the download protocol of the indexer's releases, such as usenet or torrent.
	/// </summary>
	/// <remarks>
	/// The value is a copy of the <c>Protocol</c> property, so it doesn't follow a change that PowerShell makes until a
	/// request that sends the object succeeds.
	/// </remarks>
	/// <value>The indexer's protocol, or an empty string when it has none.</value>
	public string Protocol { get; private set; } = string.Empty;

	/// <summary>
	/// Initializes a new <see cref="IndexerObject"/> instance with no properties, for the JSON converter to fill.
	/// </summary>
	public IndexerObject()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Gets the metadata tag for an indexer.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags.</param>
	/// <param name="existing">The object's current tag, which the method ignores.</param>
	/// <returns>The <see cref="Meta.INDEXER"/> tag from <paramref name="resolver"/>.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.INDEXER];
	}

	/// <summary>
	/// Refreshes <see cref="Name"/> and <see cref="Protocol"/> from the <c>Name</c> and <c>Protocol</c> properties,
	/// after a request that sends the object succeeds.
	/// </summary>
	protected override void OnCommit()
	{
		this.Name = this.GetValue<string>() ?? string.Empty;
		this.Protocol = this.GetValue<string>() ?? string.Empty;
		base.OnCommit();
	}
	/// <summary>
	/// Finishes the indexer after it's deserialized, copying the <c>Name</c> and <c>Protocol</c> properties into
	/// <see cref="Name"/> and <see cref="Protocol"/>.
	/// </summary>
	/// <param name="alreadyCalled"><see langword="true"/> if the object was deserialized before; otherwise,
	/// <see langword="false"/>.</param>
	protected override void OnDeserialized(bool alreadyCalled)
	{
		base.OnDeserialized(alreadyCalled);
		if (this.TryGetNonNullProperty(nameof(this.Name), out string? name))
		{
			this.Name = name;
		}

		if (this.TryGetNonNullProperty(nameof(this.Protocol), out string? pro))
		{
			this.Protocol = pro;
		}
	}
	/// <summary>
	/// Checks the indexer's values before the object is serialized.
	/// </summary>
	/// <remarks>
	/// A <see cref="Priority"/> outside 0 to 100 is set to 25, and a <c>Name</c> property that's
	/// <see langword="null"/>, empty, or white space gets the value of <see cref="Name"/>.
	/// </remarks>
	public void OnSerializing()
	{
		int priority = this.Priority;
		if (priority < 0 || priority > 100)
		{
			this.Priority = 25;
		}

		string? name = this.GetValue<string>(nameof(this.Name));
		if (string.IsNullOrWhiteSpace(name))
		{
			this.UpdateProperty(x => x.Name);
		}
	}
	/// <summary>
	/// Inserts the full name of <see cref="IndexerObject"/> at the start of the object's type names, ahead of the base
	/// class's name.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}

	/// <summary>
	/// Gets the ID of the indexer to test.
	/// </summary>
	/// <returns>The indexer's <see cref="IdSonarrObject{TSelf}.Id"/>.</returns>
	public int? GetId()
	{
		return this.Id;
	}
}
