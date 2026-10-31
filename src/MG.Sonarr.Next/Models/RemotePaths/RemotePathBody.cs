using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.RemotePaths;

/// <summary>
/// Represents the body of a request that creates or updates a remote path mapping.
/// </summary>
/// <remarks>
/// The body is written in the order <see cref="Id"/>, <see cref="Host"/>, <see cref="LocalPath"/>, and
/// <see cref="RemotePath"/>, and leaves out an <see cref="Id"/> that's <see langword="null"/>, as a new mapping's is.
/// </remarks>
public sealed class RemotePathBody
{
	/// <summary>
	/// The host, or <see langword="null"/> until it's set.
	/// </summary>
	string? _hostName;
	/// <summary>
	/// The local path, or <see langword="null"/> until it's set.
	/// </summary>
	string? _localPath;
	/// <summary>
	/// The remote path, or <see langword="null"/> until it's set.
	/// </summary>
	string? _remotePath;

	/// <summary>
	/// Gets or sets the ID of the mapping to update.
	/// </summary>
	/// <value>The mapping's ID, or <see langword="null"/> for a new mapping.</value>
	[JsonPropertyOrder(0)]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? Id { get; set; }

	/// <summary>
	/// Gets or sets the host of the download client that the mapping applies to.
	/// </summary>
	/// <value>The host, or an empty string when none was set.</value>
	[JsonPropertyOrder(1)]
	public string Host
	{
		get => _hostName ??= string.Empty;
		set => _hostName = value;
	}
	/// <summary>
	/// Gets or sets the local path that the remote path maps to.
	/// </summary>
	/// <value>The local path, or an empty string when none was set.</value>
	[JsonPropertyOrder(2)]
	public string LocalPath
	{
		get => _localPath ??= string.Empty;
		set => _localPath = value;
	}
	/// <summary>
	/// Gets or sets the path that the download client reports.
	/// </summary>
	/// <value>The remote path, or an empty string when none was set.</value>
	[JsonPropertyOrder(3)]
	public string RemotePath
	{
		get => _remotePath ??= string.Empty;
		set => _remotePath = value;
	}
}

