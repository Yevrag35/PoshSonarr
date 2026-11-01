namespace MG.Sonarr.Next.Models;

/// <summary>
/// Represents a Sonarr API resource that has a numeric ID.
/// </summary>
/// <remarks>
/// The converter makes the resource's <c>Id</c> property read-only, and <see cref="OnDeserialized()"/> copies it into
/// <see cref="Id"/>. Objects sort by ID unless a derived model overrides <see cref="CompareTo(TSelf)"/>.
/// </remarks>
/// <typeparam name="TSelf">The derived model type.</typeparam>
public abstract class IdSonarrObject<TSelf> : SonarrObject,
	IComparable<TSelf>,
	IHasId
	where TSelf : IdSonarrObject<TSelf>
{
	/// <summary>
	/// Gets the resource's ID.
	/// </summary>
	/// <value>
	/// The value of the object's <c>Id</c> property when it was deserialized, or 0 if it had none.
	/// </value>
	public int Id { get; private set; }
	/// <summary>
	/// Initializes a new <see cref="IdSonarrObject{TSelf}"/> instance with room for the specified number of members.
	/// </summary>
	/// <param name="capacity">
	/// The number of members that the object's member collection starts with room for, about the number of fields that
	/// Sonarr sends for the resource.
	/// </param>
	protected IdSonarrObject(int capacity)
		: base(capacity)
	{
	}

	/// <summary>
	/// Compares the object with another object of the same model by ID.
	/// </summary>
	/// <param name="other">The object to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if the object's <see cref="Id"/> is less than that of <paramref name="other"/>, 0 if they're
	/// equal, and a positive number if it's greater or <paramref name="other"/> is <see langword="null"/>.
	/// </returns>
	public virtual int CompareTo(TSelf? other)
	{
		return Comparer<int?>.Default.Compare(this.Id, other?.Id);
	}
	/// <summary>
	/// Copies the object's <c>Id</c> property into <see cref="Id"/> and finishes setting up the object.
	/// </summary>
	/// <remarks>
	/// The override is sealed, so a derived model hooks deserialization through
	/// <see cref="SonarrObject.OnDeserialized(bool)"/>.
	/// </remarks>
	public sealed override void OnDeserialized()
	{
		if (this.TryGetId(out int id))
		{
			this.Id = id;
		}

		base.OnDeserialized();
	}
}
