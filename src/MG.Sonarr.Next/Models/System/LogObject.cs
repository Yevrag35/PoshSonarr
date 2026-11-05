using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.System;

/// <summary>
/// Represents an entry in the Sonarr server's log, from <c>/log</c>.
/// </summary>
/// <remarks>
/// Log entries sort by <see cref="Time"/>.
/// </remarks>
[SonarrObject]
public sealed class LogObject : SonarrObject,
	IComparable<LogObject>,
	ISerializableNames<LogObject>
{
	/// <summary>
	/// The number of properties that a new log entry reserves room for.
	/// </summary>
	const int CAPACITY = 5;
	/// <summary>
	/// The PowerShell type name of <see cref="LogObject"/>.
	/// </summary>
	static readonly string _typeName = typeof(LogObject).GetName();

	/// <summary>
	/// Gets the entry's log level.
	/// </summary>
	/// <remarks>
	/// It's a copy of the <c>Level</c> property, taken while the entry is deserialized.
	/// </remarks>
	/// <value>The level, such as <c>info</c> or <c>warn</c>, or an empty string when Sonarr sends none.</value>
	public string Level { get; private set; } = string.Empty;
	/// <summary>
	/// Gets the time that the entry was logged.
	/// </summary>
	/// <remarks>
	/// It's a copy of the <c>Time</c> property, taken while the entry is deserialized.
	/// </remarks>
	/// <value>The entry's time, or <see langword="default"/> when Sonarr sends none.</value>
	public DateTimeOffset Time { get; private set; }

	/// <summary>
	/// Initializes a new <see cref="LogObject"/> instance with no properties.
	/// </summary>
	public LogObject()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Gets the metadata tag for log entries.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's tags.</param>
	/// <param name="existing">The tag that the object has now, which a log entry doesn't use.</param>
	/// <returns>The resolver's tag for the log endpoint.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.LogItem];
	}

	/// <summary>
	/// Compares this log entry with another by time.
	/// </summary>
	/// <param name="other">The log entry to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if this entry sorts before <paramref name="other"/>, 0 if they sort together, or a positive
	/// number if it sorts after <paramref name="other"/> or <paramref name="other"/> is <see langword="null"/>.
	/// </returns>
	public int CompareTo(LogObject? other)
	{
		return other is null ? 1 : this.Time.CompareTo(other.Time);
	}

	/// <summary>
	/// Finishes deserializing the entry: it runs the base's deserialization and copies the entry's level and time
	/// into <see cref="Level"/> and <see cref="Time"/>.
	/// </summary>
	public override void OnDeserialized()
	{
		base.OnDeserialized();
		if (this.TryGetNonNullProperty(nameof(this.Level), out string? level))
		{
			this.Level = level;
		}

		if (this.TryGetProperty(nameof(this.Time), out DateTimeOffset time))
		{
			this.Time = time;
		}
	}
	/// <summary>
	/// Inserts the <see cref="LogObject"/> type name at the start of the object's PowerShell type names.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}
}
