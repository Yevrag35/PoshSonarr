using MG.Sonarr.Next.Models.Commands;

namespace MG.Sonarr.Next.Services.Jobs;

/// <summary>
/// Represents a keyed collection of Sonarr commands.
/// </summary>
public interface ICommandHistory : IReadOnlyCollection<ICommand>
{
	///// <summary>
	///// Gets the number of commands stored in the history.
	///// </summary>
	//int Count { get; }

	/// <summary>
	/// Gets or sets the command associated with the specified identifier.
	/// </summary>
	/// <param name="id">The command identifier.</param>
	/// <value>The command associated with <paramref name="id"/>, or a default empty command when no entry exists.</value>
	ICommand this[int id] { get; set; }

	/// <summary>
	/// Adds the specified command when it has a valid identifier and is not already present.
	/// </summary>
	/// <param name="command">The command to add.</param>
	/// <returns><see langword="true"/> when the command is added; otherwise, <see langword="false"/>.</returns>
	bool Add([NotNullWhen(true)] ICommand? command);

	/// <summary>
	/// Removes the command associated with the specified identifier.
	/// </summary>
	/// <param name="id">The command identifier to remove.</param>
	/// <returns><see langword="true"/> when the command is removed; otherwise, <see langword="false"/>.</returns>
	bool Remove(int id);

	/// <summary>
	/// Attempts to get the command associated with the specified identifier.
	/// </summary>
	/// <param name="id">The command identifier to look up.</param>
	/// <param name="value">The command associated with <paramref name="id"/> when found; otherwise, <see langword="null"/>.</param>
	/// <returns><see langword="true"/> when a command is found; otherwise, <see langword="false"/>.</returns>
	bool TryGetValue(int id, [NotNullWhen(true)] out ICommand? value);

	/// <summary>
	/// Replaces the stored command that has the identifier of the specified command, when the history holds one.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The method adds nothing when the history holds no command with that identifier, while the setter of
	/// <see cref="this[int]"/> adds the command.
	/// </para>
	/// <para>
	/// The replacement happens in one step, so no caller sees the history without a command that has that identifier,
	/// as one could between a call to <see cref="Remove(int)"/> and a call to <see cref="Add(ICommand)"/>.
	/// </para>
	/// </remarks>
	/// <param name="command">The command to store in place of the one that has its identifier.</param>
	/// <returns>
	/// <see langword="true"/> if the history held a command with the identifier of <paramref name="command"/> and now
	/// holds <paramref name="command"/> in its place; otherwise, <see langword="false"/>.
	/// </returns>
	bool TryReplace([NotNullWhen(true)] ICommand? command);
}

/// <summary>
/// Represents the default in-memory implementation of <see cref="ICommandHistory"/>.
/// </summary>
file sealed class CommandHistory : ICommandHistory
{
	private static readonly ICommand s_empty = EmptyCommand.Default;
	private readonly Dictionary<int, ICommand> _dict = [];
	private readonly Lock _lock = new();

	/// <summary>
	/// Gets or sets the command associated with the specified identifier.
	/// </summary>
	/// <param name="id">The command identifier.</param>
	/// <value>The command associated with <paramref name="id"/>, or <see cref="EmptyCommand.Default"/> when no entry exists.</value>
	public ICommand this[int id]
	{
		get
		{
			lock (_lock)
			{
				return _dict.TryGetValue(id, out ICommand? command)
					? command
					: s_empty;
			}
		}
		set
		{
			lock (_lock)
			{
				_dict[id] = value;
			}
		}
	}

	/// <summary>
	/// Gets the number of commands stored in the history.
	/// </summary>
	/// <value>The number of commands currently stored.</value>
	public int Count
	{
		get
		{
			lock (_lock)
			{
				return _dict.Count;
			}
		}
	}

	/// <summary>
	/// Initializes a new <see cref="CommandHistory"/> instance.
	/// </summary>
	public CommandHistory() { }

	/// <inheritdoc cref="ICommandHistory.Add(ICommand)"/>
	public bool Add([NotNullWhen(true)] ICommand? command)
	{
		if (!IsProperCommand(command))
		{
			return false;
		}

		lock (_lock)
		{
			return _dict.TryAdd(command.Id, command);
		}
	}

	/// <summary>
	/// Determines whether the specified command is present in the history.
	/// </summary>
	/// <param name="command">The command to locate.</param>
	/// <returns><see langword="true"/> when the command is present; otherwise, <see langword="false"/>.</returns>
	public bool Contains([NotNullWhen(true)] ICommand? command)
	{
		if (!IsProperCommand(command))
		{
			return false;
		}

		lock (_lock)
		{
			return _dict.ContainsKey(command.Id);
		}
	}

	/// <summary>
	/// Determines whether a command with the specified identifier is present in the history.
	/// </summary>
	/// <param name="id">The command identifier to locate.</param>
	/// <returns><see langword="true"/> when a matching command is present; otherwise, <see langword="false"/>.</returns>
	public bool ContainsId(int id)
	{
		if (!IsProperId(in id))
		{
			return false;
		}

		lock (_lock)
		{
			return _dict.ContainsKey(id);
		}
	}

	/// <inheritdoc cref="ICommandHistory.Remove(int)"/>
	public bool Remove(int id)
	{
		if (!IsProperId(in id))
		{
			return false;
		}

		lock (_lock)
		{
			return _dict.Remove(id);
		}
	}

	/// <inheritdoc cref="ICommandHistory.TryGetValue(int, out ICommand)"/>
	public bool TryGetValue(int id, [NotNullWhen(true)] out ICommand? value)
	{
		lock (_lock)
		{
			return _dict.TryGetValue(id, out value);
		}
	}

	/// <inheritdoc cref="ICommandHistory.TryReplace(ICommand)"/>
	public bool TryReplace([NotNullWhen(true)] ICommand? command)
	{
		bool result = false;

		if (IsProperCommand(command))
		{
			lock (_lock)
			{
				ref ICommand entry = ref CollectionsMarshal.GetValueRefOrNullRef(_dict, command.Id);
				if (!Unsafe.IsNullRef(in entry))
				{
					entry = command;
					result = true;
				}
			}
		}

		return result;
	}

	/// <summary>
	/// Returns an enumerator over the commands that the history holds when the method is called.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The method copies the commands to an array under the history's lock and returns an enumerator over the copy, so
	/// the enumerator holds no lock. Other threads can change the history while a caller enumerates, and the enumerator
	/// doesn't see their changes. The commands come in no particular order.
	/// </para>
	/// <para>
	/// For an empty history, the method returns the enumerator of an empty array, which the runtime caches, so it
	/// allocates nothing. Otherwise, it allocates the copy and its enumerator.
	/// </para>
	/// </remarks>
	/// <returns>An enumerator over a copy of the stored commands.</returns>
	public IEnumerator<ICommand> GetEnumerator()
	{
		ICommand[] snapshot;
		lock (_lock)
		{
			snapshot = _dict.Count == 0 ? [] : [.. _dict.Values];
		}

		return ((IEnumerable<ICommand>)snapshot).GetEnumerator();
	}

	/// <inheritdoc cref="GetEnumerator()"/>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
	}

	/// <summary>
	/// Determines whether the specified command can be stored in the history.
	/// </summary>
	/// <param name="command">The command to evaluate.</param>
	/// <returns><see langword="true"/> when the command is not <see langword="null"/> and does not use the empty command identifier; otherwise, <see langword="false"/>.</returns>
	private static bool IsProperCommand([NotNullWhen(true)] ICommand? command)
	{
		return command is not null && command.Id != EmptyCommand.Default.Id;
	}

	/// <summary>
	/// Determines whether the specified identifier can be used for a stored command.
	/// </summary>
	/// <param name="id">The identifier to evaluate.</param>
	/// <returns><see langword="true"/> when the identifier is not the empty command identifier; otherwise, <see langword="false"/>.</returns>
	private static bool IsProperId(in int id)
	{
		return EmptyCommand.Default.Id != id;
	}
}

/// <summary>
/// Provides dependency injection registrations for command history services.
/// </summary>
internal static class CommandDependencyInjection
{
	/// <summary>
	/// Adds command history services to the specified service collection.
	/// </summary>
	/// <param name="services">The service collection to update.</param>
	/// <returns>The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.</returns>
	internal static IServiceCollection AddCommandHistory(this IServiceCollection services)
	{
		return services.AddSingleton<ICommandHistory, CommandHistory>();
	}
}
