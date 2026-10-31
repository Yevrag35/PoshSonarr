using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.System;

/// <summary>
/// Represents the Sonarr server's host settings, from <c>/config/host</c>.
/// </summary>
/// <remarks>
/// <para>
/// The settings are a single object, and its ID stays out of PowerShell's view: deserializing the object copies the
/// <c>Id</c> property into <see cref="Id"/> and removes it, <see cref="OnSerializing"/> adds it back for a request
/// body, and <see cref="Commit"/> and <see cref="Reset"/> remove it again.
/// </para>
/// <para>
/// The values of <c>AuthenticationMethod</c>, <c>CertificateValidation</c>, <c>LogLevel</c>, <c>ProxyType</c>, and
/// <c>UpdateMechanism</c> read with an uppercase first letter.
/// </para>
/// </remarks>
[SonarrObject]
public class HostObject : SonarrObject,
	IHasId,
	IJsonOnSerializing,
	ISerializableNames<HostObject>
{
	/// <summary>
	/// The number of properties that a new host config reserves room for.
	/// </summary>
	const int CAPACITY = 32;
	/// <summary>
	/// The number of values that <see cref="Conditionals"/> reserves room for.
	/// </summary>
	const int CONDITIONAL_CAPACITY = 3;
	/// <summary>
	/// The full name of the type, which <see cref="SetPSTypeName"/> inserts into the object's type names.
	/// </summary>
	static readonly string _typeName = typeof(HostObject).GetName();

	/// <summary>
	/// Gets the values of the properties that a derived class keeps out of PowerShell's view until a request body
	/// needs them.
	/// </summary>
	/// <value>A dictionary of property values by property name, whose keys ignore case.</value>
	private protected Dictionary<string, object?> Conditionals { get; }

	/// <summary>
	/// Gets the ID of the host settings.
	/// </summary>
	/// <remarks>
	/// It's a copy of the <c>Id</c> property, which deserializing the object removes.
	/// </remarks>
	/// <value>The settings' ID, or 0 when Sonarr sends none.</value>
	public int Id { get; private set; }

	/// <summary>
	/// Initializes a new <see cref="HostObject"/> instance with no properties.
	/// </summary>
	public HostObject()
		: base(CAPACITY)
	{
		this.Conditionals = new(CONDITIONAL_CAPACITY, StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Removes the <c>Id</c> property that <see cref="OnSerializing"/> added, after a request that sent the settings
	/// succeeds.
	/// </summary>
	public override void Commit()
	{
		base.Commit();
		this.Properties.Remove(nameof(this.Id));
	}
	/// <summary>
	/// Gets the metadata tag for the host settings.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's tags.</param>
	/// <param name="existing">The tag that the object has now, which the host settings don't use.</param>
	/// <returns>The resolver's tag for the host config endpoint.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.HOST];
	}
	/// <summary>
	/// Finishes deserializing the settings, copying the <c>Id</c> property into <see cref="Id"/> and removing it.
	/// </summary>
	public override void OnDeserialized()
	{
		base.OnDeserialized();
		if (this.TryGetId(out int id))
		{
			this.Id = id;
		}

		this.Properties.Remove(nameof(this.Id));
	}
	/// <summary>
	/// Adds the <c>Id</c> property back from <see cref="Id"/>, before the settings are written to a request body.
	/// </summary>
	public virtual void OnSerializing()
	{
		this.ReplaceNumberProperty(nameof(this.Id), this.Id);
	}
	/// <summary>
	/// Removes the <c>Id</c> property that <see cref="OnSerializing"/> added, after a request that sent the settings
	/// fails.
	/// </summary>
	public override void Reset()
	{
		base.Reset();
		this.Properties.Remove(nameof(this.Id));
	}
	/// <summary>
	/// Inserts the host settings' own type name ahead of the base type names.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}

	/// <summary>
	/// The names of the properties whose values read with an uppercase first letter.
	/// </summary>
	static readonly HashSet<string> _capitalProps = [
		"AuthenticationMethod", "CertificateValidation", "LogLevel", "ProxyType", "UpdateMechanism",
	];
	/// <summary>
	/// Gets the names of the string properties whose values read with an uppercase first letter.
	/// </summary>
	/// <returns>
	/// A set of <c>AuthenticationMethod</c>, <c>CertificateValidation</c>, <c>LogLevel</c>, <c>ProxyType</c>, and
	/// <c>UpdateMechanism</c>.
	/// </returns>
	public static IReadOnlySet<string> GetPropertiesToCapitalize()
	{
		return _capitalProps;
	}
}

/// <summary>
/// Represents the Sonarr server's host settings with the API key and the passwords hidden.
/// </summary>
/// <remarks>
/// <para>
/// Deserializing the object moves <c>ApiKey</c>, <c>Password</c>, and <c>ProxyPassword</c> out of its properties.
/// <see cref="OnSerializing"/> writes them back, so that a request that updates the settings keeps them,
/// <see cref="Commit"/> moves them out again, taking the values that the request sent, and <see cref="Reset"/>
/// removes them and keeps the values from before.
/// </para>
/// <para>
/// Its type name replaces <see cref="HostObject"/>'s, so it doesn't match the host settings' format views.
/// </para>
/// </remarks>
[SonarrObject]
public sealed class NoKeyHostObject : HostObject,
	IComparable<NoKeyHostObject>,
	ISerializableNames<NoKeyHostObject>
{
	/// <summary>
	/// The names of the properties that the object keeps out of PowerShell's view.
	/// </summary>
	static readonly ImmutableArray<string> _removeProperties =
	[
		Constants.API_KEY, Constants.PASSWORD, Constants.PROXY_PASSWORD,
	];
	/// <summary>
	/// The full name of the type, which <see cref="SetPSTypeName"/> puts in place of the base's type name.
	/// </summary>
	static readonly string _typeName = typeof(NoKeyHostObject).GetName();

	/// <summary>
	/// Compares this object with another through
	/// <see cref="global::System.Management.Automation.PSObject.CompareTo(object)"/>.
	/// </summary>
	/// <param name="other">The object to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if this object sorts before <paramref name="other"/>, 0 if they sort together, or a
	/// positive number if it sorts after.
	/// </returns>
	public int CompareTo(NoKeyHostObject? other)
	{
		return base.CompareTo(other);
	}

	/// <summary>
	/// Removes the <c>Id</c> property and moves the hidden values out of the properties again, after a request that
	/// sent the settings succeeds.
	/// </summary>
	/// <remarks>
	/// The values that the request sent replace the ones kept before.
	/// </remarks>
	public override void Commit()
	{
		base.Commit();
		this.Conditionals.Clear();
		this.StoreConditionals();
	}
	/// <summary>
	/// Finishes deserializing the settings and moves the API key and the passwords out of the properties.
	/// </summary>
	public override void OnDeserialized()
	{
		base.OnDeserialized();
		this.StoreConditionals();
	}
	/// <summary>
	/// Adds the <c>Id</c> property and the hidden values back, before the settings are written to a request body.
	/// </summary>
	public override void OnSerializing()
	{
		base.OnSerializing();
		foreach (var kvp in this.Conditionals)
		{
			this.UpdateProperty(kvp.Value, propertyName: kvp.Key);
		}
	}
	/// <summary>
	/// Removes the <c>Id</c> property and the hidden values that <see cref="OnSerializing"/> added, after a request
	/// that sent the settings fails.
	/// </summary>
	/// <remarks>
	/// The values kept from before the request stay kept.
	/// </remarks>
	public override void Reset()
	{
		base.Reset();
		this.Properties.RemoveMany(_removeProperties.AsSpan());
	}
	/// <summary>
	/// Keeps the values of the API key and password properties and removes the properties.
	/// </summary>
	/// <remarks>
	/// A property whose value is <see langword="null"/> is kept as an empty string, and a missing property keeps
	/// nothing.
	/// </remarks>
	private void StoreConditionals()
	{
		foreach (string property in _removeProperties)
		{
			if (this.TryGetProperty(property, out string? propValue))
			{
				this.Conditionals[property] = propValue ?? string.Empty;
			}
		}

		this.Properties.RemoveMany(_removeProperties.AsSpan());
	}
	/// <summary>
	/// Puts the object's own type name in place of <see cref="HostObject"/>'s.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();

		Debug.Assert(this.TypeNames.Count > 0 && this.TypeNames[0] == typeof(HostObject).GetName());
		this.TypeNames[0] = _typeName;
	}
}
