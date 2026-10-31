using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Services.Auth;

namespace MG.Sonarr.Next.Models.System;

/// <summary>
/// Represents the Sonarr server's status, from <c>/system/status</c>.
/// </summary>
/// <remarks>
/// The values of <c>Authentication</c>, <c>Mode</c>, <c>PackageUpdateMechanism</c>, and <c>RuntimeName</c> read with
/// an uppercase first letter.
/// </remarks>
[SonarrObject]
public sealed class SystemStatusObject : SonarrObject,
	ISerializableNames<SystemStatusObject>
{
	/// <summary>
	/// The number of properties that a new status reserves room for.
	/// </summary>
	const int CAPACITY = 26;
	/// <summary>
	/// The full name of the type, which <see cref="SetPSTypeName"/> inserts into the object's type names.
	/// </summary>
	static readonly string _typeName = typeof(SystemStatusObject).GetName();

	/// <summary>
	/// Gets the authentication method that the server uses.
	/// </summary>
	/// <remarks>
	/// It's parsed from the <c>Authentication</c> property, ignoring case, while the status is deserialized.
	/// </remarks>
	/// <value>
	/// The server's authentication method, or <see langword="default"/> when the property is missing or doesn't name
	/// a method.
	/// </value>
	public SonarrAuthType Authentication { get; private set; }

	/// <summary>
	/// Initializes a new <see cref="SystemStatusObject"/> instance with no properties.
	/// </summary>
	public SystemStatusObject()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Gets the metadata tag for the server's status.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's tags.</param>
	/// <param name="existing">The tag that the object has now, which the status doesn't use.</param>
	/// <returns>The resolver's tag for the system status endpoint.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.STATUS];
	}

	/// <summary>
	/// Finishes deserializing the status and parses its <c>Authentication</c> property into
	/// <see cref="Authentication"/>.
	/// </summary>
	public override void OnDeserialized()
	{
		base.OnDeserialized();
		if (this.TryGetProperty(nameof(this.Authentication), out string? at) && Enum.TryParse(at, true, out SonarrAuthType authType))
		{
			this.Authentication = authType;
		}
	}
	/// <summary>
	/// Inserts the status's own type name ahead of the base type names.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}

	/// <summary>
	/// The names of the properties whose values read with an uppercase first letter.
	/// </summary>
	static readonly HashSet<string> _capitalProps = new(4, StringComparer.InvariantCultureIgnoreCase)
	{
		"Authentication", "Mode", "PackageUpdateMechanism", "RuntimeName"
	};
	/// <summary>
	/// Gets the names of the string properties whose values read with an uppercase first letter.
	/// </summary>
	/// <returns>
	/// A set of <c>Authentication</c>, <c>Mode</c>, <c>PackageUpdateMechanism</c>, and <c>RuntimeName</c>, which
	/// ignores case.
	/// </returns>
	public static IReadOnlySet<string> GetPropertiesToCapitalize()
	{
		return _capitalProps;
	}
}

