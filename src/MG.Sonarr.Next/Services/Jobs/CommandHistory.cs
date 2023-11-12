using MG.Sonarr.Next.Models.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;

namespace MG.Sonarr.Next.Services.Jobs
{
    public interface ICommandHistory : IReadOnlyCollection<ICommand>
    {
        ICommand this[int id] { get; set; }

        bool Add([NotNullWhen(true)] ICommand? command);
        bool Remove(int id);
        bool TryGetValue(int id, [NotNullWhen(true)] out ICommand? value);
    }
    file sealed class CommandHistory : ICommandHistory
    {
        readonly Dictionary<int, ICommand> _dict;

        public ICommand this[int id]
        {
            get => _dict.TryGetValue(id, out ICommand? command)
                ? command
                : EmptyCommand.Default;
            set => _dict[id] = value;
        }

        public int Count => _dict.Count;
        public IReadOnlyCollection<int> Ids => _dict.Keys;

        public CommandHistory()
        {
            _dict = new();
        }

        public bool Add([NotNullWhen(true)] ICommand? command)
        {
            return IsProperCommand(command) && _dict.TryAdd(command.Id, command);
        }
        public bool Contains([NotNullWhen(true)] ICommand? command)
        {
            return IsProperCommand(command) && _dict.ContainsKey(command.Id);
        }
        public bool ContainsId(int id)
        {
            return IsProperId(in id) && _dict.ContainsKey(id);
        }
        public bool Remove(int id)
        {
            return IsProperId(in id) && _dict.Remove(id);
        }
        /// <inheritdoc cref="Dictionary{TKey, TValue}.TryGetValue(TKey, out TValue)"/>
        public bool TryGetValue(int id, [NotNullWhen(true)] out ICommand? value)
        {
            return _dict.TryGetValue(id, out value);
        }

        public IEnumerator<ICommand> GetEnumerator()
        {
            return _dict.Values.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private static bool IsProperCommand([NotNullWhen(true)] ICommand? command)
        {
            return command is not null && command.Id != EmptyCommand.Default.Id;
        }
        private static bool IsProperId(in int id)
        {
            return EmptyCommand.Default.Id != id;
        }
    }

    internal static class CommandDependencyInjection
    {
        internal static IServiceCollection AddCommandHistory(this IServiceCollection services)
        {
            return services.AddSingleton<ICommandHistory, CommandHistory>();
        }
    }
}
