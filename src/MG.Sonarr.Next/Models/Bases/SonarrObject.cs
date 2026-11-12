using MG.Sonarr.Next.Extensions.Pwsh;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.PSProperties;

namespace MG.Sonarr.Next.Models;

/// <summary>
/// Represents a Sonarr API resource as a PowerShell object.
/// </summary>
/// <remarks>
/// <para>
/// The object holds the resource's fields as note properties, which PowerShell sees in place of the object's C#
/// members.
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
	/// A value that indicates whether <see cref="OnDeserialized()"/> has run on the object and added its type names.
	/// </summary>
	private bool _addedType;
	/// <summary>
	/// The full name of <see cref="SonarrObject"/>, which <see cref="SetPSTypeName()"/> inserts.
	/// </summary>
	private const string TYPE_NAME = "MG.Sonarr.Next.Models.SonarrObject";

	/// <summary>
	/// When overridden in a derived class, gets a value that indicates whether
	/// <see cref="SetTag(IMetadataResolver)"/> leaves the object untagged.
	/// </summary>
	/// <remarks>
	/// The base implementation returns <see langword="false"/>.
	/// </remarks>
	/// <value>
	/// <see langword="true"/> if the object gets no <c>MetadataTag</c> note property; otherwise,
	/// <see langword="false"/>.
	/// </value>
	protected virtual bool DisregardMetadataTag { get; }

	/// <summary>
	/// Gets the object's metadata tag, which names its endpoint and the cmdlets that it can be piped to.
	/// </summary>
	/// <value>
	/// The tag in the object's <c>MetadataTag</c> note property, or <see cref="MetadataTag.Empty"/> if the object hasn't
	/// been tagged.
	/// </value>
	public MetadataTag MetadataTag => this.GetValue<MetadataTag>() ?? MetadataTag.Empty;

	/// <summary>
	/// Initializes a new <see cref="SonarrObject"/> instance with room for the specified number of members.
	/// </summary>
	/// <param name="capacity">
	/// The number of members that the object's member collection starts with room for, about the number of fields that
	/// Sonarr sends for the resource.
	/// </param>
	private protected SonarrObject([ConstantExpected] int capacity)
		: base(capacity)
	{
	}

	/// <summary>
	/// When overridden in a derived class, brings the object back in step after a request that sends it succeeds.
	/// </summary>
	/// <remarks>
	/// An override takes out what the model added to the object for the request body and updates the values that it
	/// keeps copies of. The base implementation does nothing.
	/// </remarks>
	public virtual void Commit()
	{
		return;
	}

	/// <summary>
	/// When implemented in a derived class, gets the metadata tag for the object.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's metadata tags.</param>
	/// <param name="existing">
	/// The tag to return when the object stands for no endpoint. <see cref="SetTag(IMetadataResolver)"/> passes
	/// <see cref="MetadataTag.Empty"/>.
	/// </param>
	/// <returns>
	/// The resolver's tag for the object's endpoint, or <paramref name="existing"/> if the object stands for no endpoint.
	/// </returns>
	protected abstract MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing);

	/// <summary>
	/// When overridden in a derived class, determines whether a parent object holds the object in a read-only
	/// property.
	/// </summary>
	/// <remarks>
	/// The base implementation returns <see langword="true"/>.
	/// </remarks>
	/// <param name="propertyName">The name of the parent's property that holds the object.</param>
	/// <param name="parentType">The type of the parent object.</param>
	/// <returns>
	/// <see langword="true"/> if the parent's property is read-only; <see langword="false"/> if it's writable.
	/// </returns>
	internal virtual bool ShouldBeReadOnly(string propertyName, Type parentType)
	{
		return true;
	}

	/// <summary>
	/// Finishes setting up the object after deserialization.
	/// </summary>
	/// <remarks>
	/// It calls <see cref="OnDeserialized(bool)"/> and then, the first time it runs on the object,
	/// <see cref="SetPSTypeName()"/>. An override calls the base implementation.
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
	/// When overridden in a derived class, finishes setting up the object after deserialization.
	/// </summary>
	/// <remarks>
	/// An override copies the values that the model's C# properties keep and adds, removes, or replaces the object's
	/// properties. The base implementation does nothing.
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
	/// An override takes out what the model added to the object for the request body. The base implementation does
	/// nothing.
	/// </remarks>
	public virtual void Reset()
	{
		return;
	}

	/// <summary>
	/// Sets the object's metadata tag from the specified resolver.
	/// </summary>
	/// <remarks>
	/// The method puts the tag that <see cref="GetTag(IMetadataResolver, MetadataTag)"/> returns in a read-only
	/// <c>MetadataTag</c> note property, replacing any that the object has, and does nothing when
	/// <see cref="DisregardMetadataTag"/> is <see langword="true"/>.
	/// </remarks>
	/// <param name="resolver">The resolver that holds the connection's metadata tags.</param>
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
	/// Inserts the full name of <see cref="SonarrObject"/> at the start of <see cref="PSObject.TypeNames"/>.
	/// </summary>
	/// <remarks>
	/// An override calls the base implementation and then inserts the model's own full name at index 0, or replaces the
	/// name at index 0 when the model derives from another model.
	/// </remarks>
	protected virtual void SetPSTypeName()
	{
		this.TypeNames.Insert(0, TYPE_NAME);
	}

	/// <summary>
	/// Gets the object's numeric ID from its <c>Id</c> property.
	/// </summary>
	/// <param name="id">
	/// When this method returns <see langword="true"/>, the value of the <c>Id</c> property; otherwise, 0.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the object has an <c>Id</c> property that holds an <see cref="int"/>; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	public bool TryGetId(out int id)
	{
		return this.TryGetProperty(Constants.Id, out id);
	}

	/// <summary>
	/// Resets the object with <see cref="Reset"/>.
	/// </summary>
	/// <returns>Always <see langword="true"/>.</returns>
	bool IResettable.TryReset()
	{
		this.Reset();
		return true;
	}
}
