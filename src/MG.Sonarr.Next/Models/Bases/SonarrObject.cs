using MG.Sonarr.Next.Collections.Pools;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.PSProperties;
using System.Management.Automation;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models;

/// <summary>
/// Represents a Sonarr API resource as a PowerShell object.
/// </summary>
/// <remarks>
/// <para>
/// The JSON converter creates a derived model as an empty custom object and adds a note property for each field of
/// the resource, so the object shows every field that Sonarr sends. PowerShell reads those note properties, not the
/// model's C# members, which exist only where C# code needs a value.
/// </para>
/// <para>
/// After it reads an object, the converter calls <see cref="OnDeserialized()"/>, and the HTTP client calls
/// <see cref="SetTag(IMetadataResolver)"/>, which adds the <see cref="MetadataTag"/> that names the object's
/// endpoint. A cmdlet calls <see cref="Commit"/> after a request that sends the object succeeds and
/// <see cref="Reset"/> after one fails.
/// </para>
/// <para>
/// Instances aren't thread-safe.
/// </para>
/// </remarks>
public abstract class SonarrObject : PSObject,
	IJsonSonarrMetadata,
	IJsonOnDeserialized,
	IResettable
{
	/// <summary>
	/// A value that indicates whether <see cref="SetPSTypeName()"/> has run.
	/// </summary>
	bool _addedType;
	/// <summary>
	/// The full type name of <see cref="SonarrObject"/>, which <see cref="SetPSTypeName()"/> inserts.
	/// </summary>
	static readonly string _typeName = typeof(SonarrObject).GetName();

	/// <summary>
	/// Gets the value of the property with the specified name.
	/// </summary>
	/// <remarks>
	/// The lookup goes through <see cref="PSObject.Properties"/>, so it finds note properties as well as the
	/// adapted members of the object.
	/// </remarks>
	/// <param name="propertyName">The name of the property, which PowerShell matches without regard to case.</param>
	/// <value>The value of the property, or <see langword="null"/> if the object has no such property.</value>
	/// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is null or empty.</exception>
	public object? this[string propertyName]
	{
		get => this.Properties[propertyName]?.Value;
	}

	/// <summary>
	/// Gets a value that indicates whether <see cref="SetTag(IMetadataResolver)"/> leaves the object untagged.
	/// </summary>
	/// <remarks>
	/// A derived model overrides it to return <see langword="true"/> when its objects stand for no endpoint, and such
	/// an object gets no <c>MetadataTag</c> property at all.
	/// </remarks>
	/// <value>
	/// <see langword="true"/> if the object gets no metadata tag; otherwise, <see langword="false"/>. The default is
	/// <see langword="false"/>.
	/// </value>
	protected virtual bool DisregardMetadataTag { get; }

	/// <summary>
	/// Gets the metadata tag that names the object's endpoint and the cmdlets it can be piped to.
	/// </summary>
	/// <remarks>
	/// The value comes from the object's <c>MetadataTag</c> note property, a <see cref="MetadataProperty"/>.
	/// </remarks>
	/// <value>
	/// The object's metadata tag, or <see cref="MetadataTag.Empty"/> if the object hasn't been tagged.
	/// </value>
	public MetadataTag MetadataTag => this.GetValue<MetadataTag>() ?? MetadataTag.Empty;

	/// <summary>
	/// Initializes a new <see cref="SonarrObject"/> instance with room for the specified number of members.
	/// </summary>
	/// <param name="capacity">
	/// The number of members that the object's member collection starts with room for, about the number of fields that
	/// Sonarr sends for the resource.
	/// </param>
	protected SonarrObject(int capacity)
		: base(capacity)
	{
	}

	/// <summary>
	/// When overridden in a derived class, brings the object back in step after a request that sends it succeeds.
	/// </summary>
	/// <remarks>
	/// A derived model takes out what it put back for the request body and updates the values that it keeps copies
	/// of. The base implementation does nothing.
	/// </remarks>
	public virtual void Commit()
	{
		return;
	}

	/// <summary>
	/// When implemented in a derived class, gets the metadata tag for the object.
	/// </summary>
	/// <remarks>
	/// A model returns the resolver's tag for its endpoint, or <paramref name="existing"/> when it stands for no
	/// endpoint.
	/// </remarks>
	/// <param name="resolver">The resolver that holds the tags that the connection registered.</param>
	/// <param name="existing">The tag to return when the object stands for no endpoint.</param>
	/// <returns>The metadata tag for the object.</returns>
	protected abstract MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing);

	/// <summary>
	/// Determines whether the object, read as a nested property of another object, is read-only.
	/// </summary>
	/// <remarks>
	/// A derived model overrides it to return <see langword="false"/> for a parent whose request body takes a value
	/// that the user sets, and the converter then makes the property writable.
	/// </remarks>
	/// <param name="propertyName">The name of the parent's property that holds the object.</param>
	/// <param name="parentType">The type of the parent object.</param>
	/// <returns>
	/// <see langword="true"/> if the property that holds the object is read-only; otherwise, <see langword="false"/>.
	/// The base implementation always returns <see langword="true"/>.
	/// </returns>
	internal virtual bool ShouldBeReadOnly(string propertyName, Type parentType)
	{
		return true;
	}

	/// <summary>
	/// Finishes setting up the object after the JSON converter has added its properties.
	/// </summary>
	/// <remarks>
	/// It calls <see cref="OnDeserialized(bool)"/>, and then, the first time only, <see cref="SetPSTypeName()"/>. A
	/// derived class that overrides it calls the base implementation.
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
	/// When overridden in a derived class, finishes setting up the object after the JSON converter has added its
	/// properties.
	/// </summary>
	/// <remarks>
	/// A derived model copies the values that its C# properties keep, removes or replaces the properties that
	/// PowerShell shouldn't show or change, and adds computed properties. The base implementation does nothing.
	/// </remarks>
	/// <param name="alreadyCalled">
	/// <see langword="true"/> if <see cref="OnDeserialized()"/> has run on the object before; otherwise,
	/// <see langword="false"/>.
	/// </param>
	protected virtual void OnDeserialized(bool alreadyCalled)
	{
		return;
	}

	/// <summary>
	/// When overridden in a derived class, brings the object back to its state before a request that sends it failed.
	/// </summary>
	/// <remarks>
	/// A derived model takes out what it put back for the request body. The base implementation does nothing.
	/// </remarks>
	public virtual void Reset()
	{
		return;
	}

	/// <summary>
	/// Adds or replaces the object's metadata tag.
	/// </summary>
	/// <remarks>
	/// It gets the tag from <see cref="GetTag(IMetadataResolver, MetadataTag)"/>, passing
	/// <see cref="MetadataTag.Empty"/> as the existing tag, and puts it in a read-only <c>MetadataTag</c> property.
	/// When <see cref="DisregardMetadataTag"/> is <see langword="true"/>, it does nothing.
	/// </remarks>
	/// <param name="resolver">The resolver that holds the tags that the connection registered.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="resolver"/> is null.</exception>
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
	/// Adds the object's type names to <see cref="PSObject.TypeNames"/>, which the format and type files select it by.
	/// </summary>
	/// <remarks>
	/// The base implementation inserts the full name of <see cref="SonarrObject"/> at index 0. A derived model calls
	/// the base implementation and then inserts its own full type name at index 0, or replaces index 0 when it derives
	/// from another model.
	/// </remarks>
	protected virtual void SetPSTypeName()
	{
		this.TypeNames.Insert(0, _typeName);
	}

	/// <summary>
	/// Gets the object's numeric ID from its <c>Id</c> property.
	/// </summary>
	/// <param name="id">
	/// When this method returns, the value of the <c>Id</c> property as an <see cref="int"/> if the object has one;
	/// otherwise, 0.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the object has an <c>Id</c> property whose value converts to an <see cref="int"/>;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	public bool TryGetId(out int id)
	{
		return this.TryGetProperty(Constants.ID, out id);
	}

	/// <summary>
	/// Resets the object through <see cref="Reset"/>.
	/// </summary>
	/// <returns>Always <see langword="true"/>.</returns>
	bool IResettable.TryReset()
	{
		this.Reset();
		return true;
	}
}
