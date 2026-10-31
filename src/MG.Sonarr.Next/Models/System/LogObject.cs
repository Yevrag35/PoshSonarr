using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
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
		return resolver[Meta.LOG_ITEM];
	}

	/// <summary>
	/// Compares this log entry with another by time.
	/// </summary>
	/// <param name="other">The log entry to compare with, or <see langword="null"/>, which compares as an entry whose time is <see langword="default"/>.</param>
	/// <returns>
	/// A negative number if this entry sorts before <paramref name="other"/>, 0 if they sort together, or a positive
	/// number if it sorts after.
	/// </returns>
	public int CompareTo(LogObject? other)
	{
		DateTimeOffset offset = other?.Time ?? default;
		return this.Time.CompareTo(offset);
	}

	/// <summary>
	/// Finishes deserializing the entry: it runs the base's deserialization and copies the entry's level and time
	/// into <see cref="Level"/> and <see cref="Time"/>.
	/// </summary>
	public override void OnDeserialized()
	{
		if (this.TryGetNonNullProperty(nameof(this.Level), out string? level))
		{
			this.Level = level;
		}

		if (this.TryGetProperty(nameof(this.Time), out DateTimeOffset time))
		{
			this.Time = time;
		}
	}
}
