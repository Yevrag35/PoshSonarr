using MG.Sonarr.Next.Collections.Pools;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.PSProperties;
using System.Management.Automation;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models;

/// <summary>
/// Provides a base PowerShell object with Sonarr-specific metadata and deserialization hooks.
/// </summary>
/// <remarks>
/// This type extends <see cref="PSObject"/> and adds support for a <see cref="MetadataTag"/> and lifecycle methods
/// used during JSON deserialization. Instances are not thread-safe.
/// </remarks>
public abstract class SonarrObject : PSObject,
	IJsonSonarrMetadata,
	IJsonOnDeserialized,
	IResettable
{
	bool _addedType;
	static readonly string _typeName = typeof(SonarrObject).GetName();

	/// <summary>
	/// Gets the value of the dynamic property with the specified name.
	/// </summary>
	/// <remarks>
	/// The lookup is performed against the PowerShell extended members in <see cref="PSObject.Properties"/>.
	/// The returned reference is the underlying property value; it may be <see langword="null"/>.
	/// </remarks>
	/// <param name="propertyName">The property name to retrieve. Must not be <see langword="null"/>.</param>
	/// <value>The value of the property, or <see langword="null"/> if the property is not found.</value>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/>.</exception>
	public object? this[string propertyName]
	{
		get => this.Properties[propertyName]?.Value;
	}

	/// <summary>
	/// Gets a value indicating whether to skip adding or updating the metadata tag.
	/// </summary>
	/// <remarks>
	/// When <see langword="true"/>, <see cref="SetTag(IMetadataResolver)"/> returns without modifying the object's metadata.
	/// Derived types can override to opt out of metadata tagging.
	/// </remarks>
	/// <value><see langword="true"/> to disregard the metadata tag; otherwise, <see langword="false"/>.</value>
	protected virtual bool DisregardMetadataTag { get; }

	/// <summary>
	/// Gets the metadata tag associated with this object.
	/// </summary>
	/// <remarks>
	/// If the object has not been tagged, this returns <see cref="MetadataTag.Empty"/>.
	/// </remarks>
	/// <value>The metadata tag that describes the originating Sonarr endpoint and pipeline capabilities.</value>
	public MetadataTag MetadataTag => this.GetValue<MetadataTag>() ?? MetadataTag.Empty;

	/// <summary>
	/// Initializes a new instance of the <see cref="SonarrObject"/> class with the specified capacity.
	/// </summary>
	/// <remarks>
	/// The <paramref name="capacity"/> is forwarded to the base <see cref="PSObject"/> to size internal collections.
	/// </remarks>
	/// <param name="capacity">The initial capacity for internal storage.</param>
	protected SonarrObject(int capacity)
		: base(capacity)
	{
	}

	/// <summary>
	/// Commit pending changes to this object.
	/// </summary>
	/// <remarks>
	/// The default implementation is a no-op. Derived types may persist buffered state.
	/// </remarks>
	public virtual void Commit()
	{
		return;
	}

	/// <summary>
	/// Get the metadata tag for this object using the provided resolver.
	/// </summary>
	/// <remarks>
	/// Implementations should return a non-empty <see cref="MetadataTag"/> when possible.
	/// <paramref name="existing"/> provides the current tag (or <see cref="MetadataTag.Empty"/>) to consider.
	/// </remarks>
	/// <param name="resolver">The metadata resolver to query for tags.</param>
	/// <param name="existing">The existing tag to consider as a baseline.</param>
	/// <returns>The resolved <see cref="MetadataTag"/>.</returns>
	protected abstract MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing);

	/// <summary>
	/// Determine whether a property should be treated as read-only in the current context.
	/// </summary>
	/// <remarks>
	/// The default implementation returns <see langword="true"/> for all properties.
	/// </remarks>
	/// <param name="propertyName">The property name to evaluate.</param>
	/// <param name="parentType">The parent type that owns the property.</param>
	/// <returns><see langword="true"/> if the property should be read-only; otherwise, <see langword="false"/>.</returns>
	internal virtual bool ShouldBeReadOnly(string propertyName, Type parentType)
	{
		return true;
	}

	/// <summary>
	/// Run post-deserialization initialization.
	/// </summary>
	/// <remarks>
	/// Invokes <see cref="OnDeserialized(bool)"/> with a flag indicating prior invocation and ensures the PowerShell
	/// type name is added once via <see cref="SetPSTypeName()"/>.
	/// </remarks>
	public virtual void OnDeserialized()
	{
		this.OnDeserialized(_addedType);
		if (_addedType)
		{
			return;
		}

		this.SetPSTypeName();
		_addedType = true;
	}

	/// <summary>
	/// Run post-deserialization initialization with an indication of prior invocation.
	/// </summary>
	/// <remarks>
	/// The default implementation is a no-op. Derived types can use <paramref name="alreadyCalled"/> to guard work.
	/// </remarks>
	/// <param name="alreadyCalled"><see langword="true"/> if this method was already called; otherwise, <see langword="false"/>.</param>
	protected virtual void OnDeserialized(bool alreadyCalled)
	{
		return;
	}

	/// <summary>
	/// Reset the object to its initial state.
	/// </summary>
	/// <remarks>
	/// The default implementation is a no-op. Derived types should clear cached state and remove ephemeral data.
	/// </remarks>
	public virtual void Reset()
	{
		return;
	}

	/// <summary>
	/// Set the metadata tag on this object using the specified resolver.
	/// </summary>
	/// <remarks>
	/// When <see cref="DisregardMetadataTag"/> is <see langword="true"/>, the method returns without changes.
	/// Otherwise, this updates or adds a <see cref="MetadataProperty"/> with the resolved tag.
	/// </remarks>
	/// <param name="resolver">The metadata resolver used to find the appropriate tag. Must not be <see langword="null"/>.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="resolver"/> is <see langword="null"/>.</exception>
	public void SetTag(IMetadataResolver resolver)
	{
		ArgumentNullException.ThrowIfNull(resolver);
		if (this.DisregardMetadataTag)
		{
			return;
		}

		MetadataTag tagToUse = this.GetTag(resolver, MetadataTag.Empty);
		this.AddOrUpdate(MetadataProperty.Empty.Name, replaceReadOnly: true, tagToUse, static (name, tag) => new MetadataProperty(tag));
	}

	/// <summary>
	/// Set the PowerShell type name for this object.
	/// </summary>
	/// <remarks>
	/// Inserts the type name for <see cref="SonarrObject"/> at index 0 of <see cref="PSObject.TypeNames"/> to influence formatting and type inference.
	/// </remarks>
	protected virtual void SetPSTypeName()
	{
		this.TypeNames.Insert(0, _typeName);
	}

	/// <summary>
	/// Try to get the numeric identifier for this object.
	/// </summary>
	/// <remarks>
	/// This looks for a property named by <see cref="Constants.ID"/> and attempts to parse it as an <see cref="int"/>.
	/// </remarks>
	/// <param name="id">When this method returns, contains the identifier if found; otherwise, 0.</param>
	/// <returns><see langword="true"/> if an identifier was found; otherwise, <see langword="false"/>.</returns>
	public bool TryGetId(out int id)
	{
		return this.TryGetProperty(Constants.ID, out id);
	}

	/// <inheritdoc/>
	bool IResettable.TryReset()
	{
		this.Reset();
		return true;
	}
}
