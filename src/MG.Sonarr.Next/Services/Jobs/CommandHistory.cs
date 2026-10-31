using MG.Sonarr.Next.Models.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Services.Jobs;

/// <summary>
/// Represents a keyed collection of Sonarr commands.
/// </summary>
public interface ICommandHistory : IReadOnlyCollection<ICommand>
{
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
}

/// <summary>
/// Represents the default in-memory implementation of <see cref="ICommandHistory"/>.
/// </summary>
file sealed class CommandHistory : ICommandHistory
{
	readonly Dictionary<int, ICommand> _dict;

	/// <summary>
	/// Gets or sets the command associated with the specified identifier.
	/// </summary>
	/// <param name="id">The command identifier.</param>
	/// <value>The command associated with <paramref name="id"/>, or <see cref="EmptyCommand.Default"/> when no entry exists.</value>
	public ICommand this[int id]
	{
		get => _dict.TryGetValue(id, out ICommand? command)
			? command
			: EmptyCommand.Default;
		set => _dict[id] = value;
	}

	/// <summary>
	/// Gets the number of commands stored in the history.
	/// </summary>
	/// <value>The number of commands currently stored.</value>
	public int Count => _dict.Count;

	/// <summary>
	/// Gets the identifiers of commands currently stored in the history.
	/// </summary>
	/// <value>A read-only collection of command identifiers.</value>
	public IReadOnlyCollection<int> Ids => _dict.Keys;

	/// <summary>
	/// Initializes a new <see cref="CommandHistory"/> instance.
	/// </summary>
	public CommandHistory()
	{
		_dict = new();
	}

	/// <inheritdoc cref="ICommandHistory.Add(ICommand)"/>
	public bool Add([NotNullWhen(true)] ICommand? command)
	{
		return IsProperCommand(command) && _dict.TryAdd(command.Id, command);
	}

	/// <summary>
	/// Determines whether the specified command is present in the history.
	/// </summary>
	/// <param name="command">The command to locate.</param>
	/// <returns><see langword="true"/> when the command is present; otherwise, <see langword="false"/>.</returns>
	public bool Contains([NotNullWhen(true)] ICommand? command)
	{
		return IsProperCommand(command) && _dict.ContainsKey(command.Id);
	}

	/// <summary>
	/// Determines whether a command with the specified identifier is present in the history.
	/// </summary>
	/// <param name="id">The command identifier to locate.</param>
	/// <returns><see langword="true"/> when a matching command is present; otherwise, <see langword="false"/>.</returns>
	public bool ContainsId(int id)
	{
		return IsProperId(in id) && _dict.ContainsKey(id);
	}

	/// <inheritdoc cref="ICommandHistory.Remove(int)"/>
	public bool Remove(int id)
	{
		return IsProperId(in id) && _dict.Remove(id);
	}

	/// <inheritdoc cref="ICommandHistory.TryGetValue(int, out ICommand)"/>
	public bool TryGetValue(int id, [NotNullWhen(true)] out ICommand? value)
	{
		return _dict.TryGetValue(id, out value);
	}

	/// <summary>
	/// Returns an enumerator that iterates through the stored commands.
	/// </summary>
	/// <returns>An enumerator for the stored command values.</returns>
	public IEnumerator<ICommand> GetEnumerator()
	{
		return _dict.Values.GetEnumerator();
	}

	/// <summary>
	/// Returns an enumerator that iterates through the stored commands.
	/// </summary>
	/// <returns>An enumerator for the stored command values.</returns>
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
