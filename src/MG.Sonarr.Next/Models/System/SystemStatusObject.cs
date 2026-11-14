using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.Pwsh;
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
	private const int CAPACITY = 26;
	/// <summary>
	/// The full name of the type, which <see cref="SetPSTypeName"/> inserts into the object's type names.
	/// </summary>
	private static readonly string _typeName = typeof(SystemStatusObject).GetName();

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
	/// Gets the character that separates the directories of a path on the server.
	/// </summary>
	/// <remarks>
	/// It's read from the <c>IsWindows</c> property, which Sonarr sets for a server that runs on Windows.
	/// </remarks>
	/// <value>
	/// A backslash when the server runs on Windows; otherwise, a forward slash.
	/// </value>
	public char DirectorySeparator => this.GetValue<bool>("IsWindows") ? '\\' : '/';
	/// <summary>
	/// Gets the URL base that the server reports.
	/// </summary>
	/// <remarks>
	/// It reads the <c>UrlBase</c> property.
	/// </remarks>
	/// <value>
	/// The server's URL base, such as <c>/sonarr</c>, an empty string for a server without one, or
	/// <see langword="null"/> when the property is missing.
	/// </value>
	public string? UrlBase => this.GetValue<string>();

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
		return resolver[Meta.Status];
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
	private static readonly HashSet<string> _capitalProps = new(4, StringComparer.OrdinalIgnoreCase)
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

