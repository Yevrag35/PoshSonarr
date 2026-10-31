using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Management.Automation;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Commands;

/// <summary>
/// Represents a command that Sonarr has queued, is running, or has run.
/// </summary>
/// <remarks>
/// <para>
/// The object keeps C# copies of the <c>Name</c>, <c>Started</c>, <c>Ended</c>, and <c>Status</c> properties as they
/// were when it was deserialized, and the copies don't follow a change that PowerShell makes to the properties
/// afterward.
/// </para>
/// <para>
/// The command's <c>Body</c> is held back from PowerShell. <see cref="OnSerializing"/> writes it into a request body,
/// and <see cref="Commit"/> and <see cref="Reset"/> remove it again. The <c>Priority</c>, <c>Status</c>, and
/// <c>Trigger</c> values get an uppercase first letter.
/// </para>
/// <para>
/// Instances aren't thread-safe.
/// </para>
/// </remarks>
[SonarrObject]
public sealed class CommandObject : IdSonarrObject<CommandObject>,
	ICommand,
	IEquatable<CommandObject>,
	IJsonOnSerializing,
	ISerializableNames<CommandObject>
{
	/// <summary>
	/// The name of the property that holds the command's body.
	/// </summary>
	const string BODY = "Body";
	/// <summary>
	/// The number of properties that a command is created with room for.
	/// </summary>
	const int CAPACITY = 16;
	/// <summary>
	/// The status that a completed command has.
	/// </summary>
	const string COMPLETED = "completed";
	/// <summary>
	/// The PowerShell type name of <see cref="CommandObject"/>.
	/// </summary>
	static readonly string _typeName = typeof(CommandObject).GetName();

	/// <summary>
	/// The command's body, held back from PowerShell until the object is serialized.
	/// </summary>
	PSObject? _body;

	/// <summary>
	/// Gets the time that the command ended.
	/// </summary>
	/// <value>The time that the command ended, or <see langword="null"/> if it hasn't ended.</value>
	public DateTimeOffset? Ended { get; private set; }
	/// <summary>
	/// Gets a value that indicates whether the command has completed.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if <see cref="Status"/> is <c>completed</c>, ignoring case; otherwise,
	/// <see langword="false"/>.
	/// </value>
	public bool IsCompleted => COMPLETED.Equals(this.Status, StringComparison.OrdinalIgnoreCase);
	/// <summary>
	/// Gets the command's name.
	/// </summary>
	/// <value>The command's name, or an empty string if Sonarr sent none.</value>
	public string Name { get; private set; } = string.Empty;
	/// <summary>
	/// Gets the time that the command started.
	/// </summary>
	/// <value>The time that the command started, or <see cref="DateTimeOffset.MinValue"/> if Sonarr sent none.</value>
	public DateTimeOffset Started { get; private set; }
	/// <summary>
	/// Gets the command's status.
	/// </summary>
	/// <value>The command's status, such as <c>Queued</c> or <c>Completed</c>, or an empty string if Sonarr sent none.</value>
	public string Status { get; private set; } = string.Empty;

	/// <summary>
	/// Initializes a new <see cref="CommandObject"/> instance with no properties.
	/// </summary>
	public CommandObject()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// The command that stands for no command.
	/// </summary>
	/// <remarks>
	/// It has an <see cref="ICommand.Id"/> of 0, the name <c>N/A</c>, and a <see cref="ICommand.Started"/> of
	/// <see cref="DateTimeOffset.MinValue"/>, and it never completes.
	/// </remarks>
	public static readonly ICommand Empty = EmptyCommand.Default;

	/// <summary>
	/// Determines whether a command has the same ID, start time, and name as another command.
	/// </summary>
	/// <param name="thisAsCommand">The command to compare.</param>
	/// <param name="otherCommand">The command to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="otherCommand"/> isn't <see langword="null"/> and has the same ID and
	/// start time as <paramref name="thisAsCommand"/> and the same name, ignoring case; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	private static bool AreCommandPropertiesEqual(CommandObject thisAsCommand, [NotNullWhen(true)] ICommand? otherCommand)
	{
		return thisAsCommand.Id.IsEqualTo(otherCommand?.Id)
			   &&
			   // Safe to access directly because other cannot be null if the first part is true.
			   thisAsCommand.Started == otherCommand.Started
			   &&
			   StringComparer.InvariantCultureIgnoreCase.Equals(thisAsCommand.Name, otherCommand.Name);
	}
	/// <summary>
	/// Determines whether a command has the same ID, start and end times, name, and status as another command.
	/// </summary>
	/// <param name="this">The command to compare.</param>
	/// <param name="other">The command to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="other"/> isn't <see langword="null"/> and has the same ID, start time,
	/// and end time as <paramref name="this"/> and the same name and status, ignoring case; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	private static bool AreCommandPropertiesEqual(CommandObject @this, CommandObject? other)
	{
		return AreCommandPropertiesEqual(thisAsCommand: @this, otherCommand: other)
			   &&
			   // Safe to access directly because other cannot be null if the first part is true.
			   @this.Ended == other.Ended
			   &&
			   StringComparer.InvariantCultureIgnoreCase.Equals(@this.Status, other.Status);
	}
	/// <summary>
	/// Removes the command's body from the object after a request that sent it succeeds.
	/// </summary>
	public override void Commit()
	{
		this.Properties.Remove(BODY);
		base.Commit();
	}
	/// <summary>
	/// Compares the command with another command by ID.
	/// </summary>
	/// <param name="other">The command to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if the command's ID comes before the ID of <paramref name="other"/>, 0 if they're equal, and a
	/// positive number if it comes after it or <paramref name="other"/> is <see langword="null"/>.
	/// </returns>
	public int CompareTo(ICommand? other)
	{
		return Comparer<int?>.Default.Compare(this.Id, other?.Id);
	}
	/// <summary>
	/// Determines whether the command equals another command object.
	/// </summary>
	/// <param name="other">The command to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="other"/> is this instance, or has the same ID, start and end times,
	/// name, and status, with the name and status compared ignoring case; otherwise, <see langword="false"/>.
	/// </returns>
	public bool Equals([NotNullWhen(true)] CommandObject? other)
	{
		return ReferenceEquals(this, other) || AreCommandPropertiesEqual(this, other);
	}
	/// <summary>
	/// Determines whether the command equals another command.
	/// </summary>
	/// <param name="command">The command to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="command"/> is this instance, or has the same ID, start and end times,
	/// name, and status, with the name and status compared ignoring case; otherwise, <see langword="false"/>.
	/// </returns>
	public bool Equals([NotNullWhen(true)] ICommand? command)
	{
		return ReferenceEquals(this, command) || AreCommandPropertiesEqual(this, command);
	}
	/// <summary>
	/// Determines whether the command equals another object.
	/// </summary>
	/// <param name="obj">The object to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="obj"/> is a <see cref="CommandObject"/> that
	/// <see cref="Equals(CommandObject)"/> finds equal, or another <see cref="ICommand"/> that
	/// <see cref="Equals(ICommand)"/> finds equal; otherwise, <see langword="false"/>.
	/// </returns>
	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		return obj switch
		{
			CommandObject co => this.Equals(co),
			ICommand command => this.Equals(command),
			_ => false,
		};
	}
	/// <summary>
	/// Gets the command's hash code.
	/// </summary>
	/// <returns>
	/// A hash code made from the command's ID, start and end times, and its name and status, ignoring case.
	/// </returns>
	public override int GetHashCode()
	{
		return HashCode.Combine(
			this.Id,
			this.Ended.GetValueOrDefault(),
			StringComparer.InvariantCultureIgnoreCase.GetHashCode(this.Name),
			this.Started,
			StringComparer.InvariantCultureIgnoreCase.GetHashCode(this.Status));
	}
	/// <summary>
	/// Gets the metadata tag of the command endpoint.
	/// </summary>
	/// <param name="resolver">The resolver that holds the tags.</param>
	/// <param name="existing">The tag that the object already has, which the command doesn't use.</param>
	/// <returns>The <see cref="Meta.COMMAND"/> tag from <paramref name="resolver"/>.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return resolver[Meta.COMMAND];
	}
	/// <summary>
	/// Holds the command's body back from PowerShell and copies the properties that the C# members keep.
	/// </summary>
	/// <param name="alreadyCalled">
	/// <see langword="true"/> if the object has been deserialized before; otherwise, <see langword="false"/>.
	/// </param>
	protected override void OnDeserialized(bool alreadyCalled)
	{
		base.OnDeserialized(alreadyCalled);
		if (this.TryGetProperty(BODY, out PSObject? body))
		{
			_body = body;
			this.Properties.Remove(BODY);
		}

		if (this.TryGetNonNullProperty(Constants.NAME, out string? name))
		{
			this.Name = name;
		}

		if (this.TryGetProperty(nameof(this.Started), out DateTimeOffset started))
		{
			this.Started = started;
		}

		if (this.TryGetProperty(nameof(this.Ended), out DateTimeOffset finished))
		{
			this.Ended = finished;
		}

		if (this.TryGetNonNullProperty(nameof(this.Status), out string? status))
		{
			this.Status = status;
		}
	}
	/// <summary>
	/// Adds the command's body back to the object before it's serialized.
	/// </summary>
	public void OnSerializing()
	{
		this.AddProperty(BODY, _body);
	}
	/// <summary>
	/// Removes the command's body from the object after a request that sent it fails.
	/// </summary>
	public override void Reset()
	{
		this.Properties.Remove(BODY);
		base.Reset();
	}
	/// <summary>
	/// Inserts the <see cref="CommandObject"/> type name at the start of the object's PowerShell type names.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}

	/// <summary>
	/// The names of the properties whose values get an uppercase first letter.
	/// </summary>
	static readonly HashSet<string> _capitalProps =
	[
		"Priority", "Status", "Trigger",
	];
	/// <summary>
	/// Gets the names of the string properties whose values get an uppercase first letter.
	/// </summary>
	/// <returns>A set that holds <c>Priority</c>, <c>Status</c>, and <c>Trigger</c>.</returns>
	public static IReadOnlySet<string> GetPropertiesToCapitalize()
	{
		return _capitalProps;
	}

	/// <summary>
	/// Determines whether two command objects are equal.
	/// </summary>
	/// <param name="x">The first command, or <see langword="null"/>.</param>
	/// <param name="y">The second command, or <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if both are <see langword="null"/>, or if <paramref name="x"/> equals
	/// <paramref name="y"/> through <see cref="Equals(CommandObject)"/>; otherwise, <see langword="false"/>.
	/// </returns>
	public static bool operator ==(CommandObject? x, CommandObject? y)
	{
		return x.IsEqualTo<CommandObject>(y);
	}
	/// <summary>
	/// Determines whether two command objects aren't equal.
	/// </summary>
	/// <param name="x">The first command, or <see langword="null"/>.</param>
	/// <param name="y">The second command, or <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="x"/> and <paramref name="y"/> aren't equal; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	public static bool operator !=(CommandObject? x, CommandObject? y)
	{
		return !(x == y);
	}
	/// <summary>
	/// Determines whether a command object equals another command.
	/// </summary>
	/// <param name="x">The command object, or <see langword="null"/>.</param>
	/// <param name="y">The command to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if both are <see langword="null"/>, or if <paramref name="x"/> equals
	/// <paramref name="y"/> through <see cref="Equals(ICommand)"/>; otherwise, <see langword="false"/>.
	/// </returns>
	public static bool operator ==(CommandObject? x, ICommand? y)
	{
		return x.IsEqualTo(y);
	}
	/// <summary>
	/// Determines whether a command object doesn't equal another command.
	/// </summary>
	/// <param name="x">The command object, or <see langword="null"/>.</param>
	/// <param name="y">The command to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="x"/> and <paramref name="y"/> aren't equal; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	public static bool operator !=(CommandObject? x, ICommand? y)
	{
		return !(x == y);
	}
	/// <summary>
	/// Determines whether a command equals a command object.
	/// </summary>
	/// <param name="x">The command, or <see langword="null"/>.</param>
	/// <param name="y">The command object to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if both are <see langword="null"/>, or if <paramref name="x"/> isn't
	/// <see langword="null"/> and its <see cref="IEquatable{T}.Equals(T)"/> finds <paramref name="y"/> equal;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	public static bool operator ==(ICommand? x, CommandObject? y)
	{
		return x.IsEqualTo(y);
	}
	/// <summary>
	/// Determines whether a command doesn't equal a command object.
	/// </summary>
	/// <param name="x">The command, or <see langword="null"/>.</param>
	/// <param name="y">The command object to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="x"/> and <paramref name="y"/> aren't equal; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	public static bool operator !=(ICommand? x, CommandObject? y)
	{
		return !(x == y);
	}
}
