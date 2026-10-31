using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.System;

/// <summary>
/// Specifies which of Sonarr's logs a log file belongs to.
/// </summary>
public enum LogFileType
{
	/// <summary>
	/// The general log, from <c>/log/file</c>.
	/// </summary>
	General,
	/// <summary>
	/// The update log, from <c>/log/file/update</c>.
	/// </summary>
	Update,
}

/// <summary>
/// Represents one of the Sonarr server's log files.
/// </summary>
/// <remarks>
/// <para>
/// Deserializing the file removes its <c>Id</c> property and sets <see cref="Type"/> from its
/// <see cref="ContentsUrl"/>.
/// </para>
/// <para>
/// Log files sort by <see cref="LastWriteTime"/> and then by <see cref="ContentsUrl"/>, ignoring case.
/// </para>
/// </remarks>
[SonarrObject]
public sealed class LogFileObject : IdSonarrObject<LogFileObject>,
	IEquatable<LogFileObject>,
	ISerializableNames<LogFileObject>
{
	/// <summary>
	/// The number of properties that a new log file reserves room for.
	/// </summary>
	const int CAPACITY = 5;
	/// <summary>
	/// The part of an update log's contents URL that sets it apart from a general log's.
	/// </summary>
	const string UPDATE_PART = "/file/update/";

	/// <summary>
	/// Gets the URL of the file's contents.
	/// </summary>
	/// <remarks>
	/// It's a copy of the <c>ContentsUrl</c> property, taken while the file is deserialized.
	/// </remarks>
	/// <value>The URL that Sonarr serves the file's contents from, or an empty string when Sonarr sends none.</value>
	public string ContentsUrl { get; private set; } = string.Empty;
	/// <summary>
	/// Gets the URL to download the file from.
	/// </summary>
	/// <value>The <c>DownloadUrl</c> property's value, or an empty string when it's missing or null.</value>
	public string DownloadUrl => this.GetStringOrEmpty();
	/// <summary>
	/// Gets the time that the file was last written to.
	/// </summary>
	/// <remarks>
	/// It's a copy of the <c>LastWriteTime</c> property, taken while the file is deserialized.
	/// </remarks>
	/// <value>The last write time, or <see langword="default"/> when Sonarr sends none.</value>
	public DateTimeOffset LastWriteTime { get; private set; }
	/// <summary>
	/// Gets or sets which log the file belongs to.
	/// </summary>
	/// <remarks>
	/// It reads and writes the <c>Type</c> property, and setting it adds the property when it's missing.
	/// </remarks>
	/// <value>
	/// <see cref="LogFileType.Update"/> for a file whose contents URL holds <c>/file/update/</c>; otherwise,
	/// <see cref="LogFileType.General"/>.
	/// </value>
	public LogFileType Type
	{
		get => this.GetValue<LogFileType>();
		set => this.SetValue(value);
	}

	/// <summary>
	/// Initializes a new <see cref="LogFileObject"/> instance with no properties.
	/// </summary>
	public LogFileObject()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Compares this log file with another by last write time and then by contents URL.
	/// </summary>
	/// <param name="other">The log file to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if this file sorts before <paramref name="other"/>, 0 if they sort together, or a positive
	/// number if it sorts after. URLs compare by the invariant culture, ignoring case.
	/// </returns>
	public override int CompareTo(LogFileObject? other)
	{
		int compare = Comparer<DateTimeOffset?>.Default.Compare(this.LastWriteTime, other?.LastWriteTime);
		if (compare == 0)
		{
			compare = StringComparer.InvariantCultureIgnoreCase.Compare(this.ContentsUrl, other?.ContentsUrl);
		}

		return compare;
	}
	/// <summary>
	/// Determines whether this log file is the same as another.
	/// </summary>
	/// <param name="other">The log file to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="other"/> is this object, or has the same <see cref="IdSonarrObject{TSelf}.Id"/>
	/// and <see cref="Type"/> and a <see cref="ContentsUrl"/> that's equal ignoring case; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	public bool Equals(LogFileObject? other)
	{
		if (ReferenceEquals(this, other))
		{
			return true;
		}
		else if (other is null)
		{
			return false;
		}

		return this.Id == other.Id
			   &&
			   this.Type == other.Type
			   &&
			   StringComparer.InvariantCultureIgnoreCase.Equals(this.ContentsUrl, other.ContentsUrl);
	}
	/// <summary>
	/// Determines whether this log file is the same as another object.
	/// </summary>
	/// <param name="obj">The object to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="obj"/> is a <see cref="LogFileObject"/> that
	/// <see cref="Equals(LogFileObject)"/> finds the same; otherwise, <see langword="false"/>.
	/// </returns>
	public override bool Equals(object? obj)
	{
		if (ReferenceEquals(this, obj))
		{
			return true;
		}
		else if (obj is LogFileObject lfo)
		{
			return this.Equals(lfo);
		}
		else
		{
			return false;
		}
	}
	/// <summary>
	/// Returns a hash code for the log file that agrees with <see cref="Equals(LogFileObject)"/>.
	/// </summary>
	/// <returns>A hash code of the file's ID, type, and contents URL, with the URL hashed ignoring case.</returns>
	public override int GetHashCode()
	{
		return HashCode.Combine(
			this.Id,
			this.Type,
			this.LastWriteTime,
			StringComparer.InvariantCultureIgnoreCase.GetHashCode(this.ContentsUrl));
	}

	/// <summary>
	/// Gets the metadata tag for log files.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's tags.</param>
	/// <param name="existing">The tag that the object has now, which a log file doesn't use.</param>
	/// <returns>The resolver's tag for the log file endpoint.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.LOG_FILE];
	}

	/// <summary>
	/// Removes the file's <c>Id</c> property, copies its contents URL and last write time, and sets
	/// <see cref="Type"/> from the URL.
	/// </summary>
	/// <param name="alreadyCalled">
	/// <see langword="true"/> if the object has been deserialized before; otherwise, <see langword="false"/>.
	/// </param>
	protected override void OnDeserialized(bool alreadyCalled)
	{
		base.OnDeserialized(alreadyCalled);
		this.Properties.Remove(nameof(this.Id));

		if (this.TryGetNonNullProperty(nameof(this.ContentsUrl), out string? cl))
		{
			this.ContentsUrl = cl;
			this.Type = cl.Contains(UPDATE_PART, StringComparison.InvariantCultureIgnoreCase)
				? LogFileType.Update
				: LogFileType.General;
		}

		if (this.TryGetProperty(nameof(this.LastWriteTime), out DateTimeOffset lastWriteTime))
		{
			this.LastWriteTime = lastWriteTime;
		}
	}
}
