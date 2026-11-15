using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Shell.Components;

/// <summary>
/// Represents the body of the request that <c>Rename-SonarrTag</c> sends: the ID of a tag and its new label.
/// </summary>
/// <remarks>
/// The body is written as JSON with the tag's <see cref="Id"/> and <see cref="Label"/>; <see cref="IsEmpty"/> is never
/// written. <see cref="Create(int, string)"/> makes an instance, and the <see langword="default"/> instance is empty,
/// with an ID of 0 and an empty label.
/// </remarks>
public readonly struct TagRename
{
	private readonly string? _name;
	private readonly int _id;
	private readonly bool _isNotEmpty;

	/// <summary>
	/// Gets the ID of the tag to rename.
	/// </summary>
	/// <value>The tag's ID, or 0 for the <see langword="default"/> instance.</value>
	public int Id => _id;

	/// <summary>
	/// Gets a value that indicates whether the instance is the <see langword="default"/> one, which renames no tag.
	/// </summary>
	/// <value>
	/// <see langword="true"/> for the <see langword="default"/> instance; <see langword="false"/> for one that
	/// <see cref="Create(int, string)"/> made.
	/// </value>
	[JsonIgnore(Condition = JsonIgnoreCondition.Always)]
	[MemberNotNullWhen(false, nameof(_name))]
	public bool IsEmpty => !_isNotEmpty;

	/// <summary>
	/// Gets the tag's new label.
	/// </summary>
	/// <value>The new label, or an empty string for the <see langword="default"/> instance.</value>
	public string Label => _name ?? string.Empty;

	/// <summary>
	/// Initializes a new <see cref="TagRename"/> instance with the specified tag ID and new label.
	/// </summary>
	/// <param name="id">The ID of the tag to rename.</param>
	/// <param name="newLabel">The tag's new label.</param>
	private TagRename(in int id, string newLabel)
	{
		_isNotEmpty = true;
		_name = newLabel;
		_id = id;
	}

	/// <summary>
	/// Creates a <see cref="TagRename"/> that gives the tag with the specified ID a new label.
	/// </summary>
	/// <remarks>
	/// The method doesn't check <paramref name="id"/>; <c>Rename-SonarrTag</c> validates it as a positive number.
	/// </remarks>
	/// <param name="id">The ID of the tag to rename.</param>
	/// <param name="newLabel">The tag's new label, which can't be null or empty.</param>
	/// <returns>The rename, which isn't empty.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="newLabel"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="newLabel"/> is empty.</exception>
	public static TagRename Create(int id, string newLabel)
	{
		ArgumentException.ThrowIfNullOrEmpty(newLabel);

		return new(id, newLabel);
	}
}

