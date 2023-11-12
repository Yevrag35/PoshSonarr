using MG.Sonarr.Next.Extensions;

namespace MG.Sonarr.Next.Models.Commands
{
    internal readonly struct EmptyCommand : ICommand
    {
        const string NA = "N/A";

        public DateTimeOffset? Ended => null;
        public int Id => 0;
        public bool IsCompleted => false;
        public string Name => NA;
        public DateTimeOffset Started => DateTimeOffset.MinValue;

        internal static readonly EmptyCommand Default = default;

        public int CompareTo(ICommand? other)
        {
            return Comparer<int?>.Default.Compare(this.Id, other?.Id);
        }

        public bool Equals(ICommand? other)
        {
            if (other is EmptyCommand)
            {
                return true;
            }

            return this.Id.IsEqualTo(other?.Id)
                   &&
                   this.Started == other.Started
                   &&
                   !other.Ended.HasValue
                   &&
                   !other.IsCompleted;
        }
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj switch
            {
                EmptyCommand => true,
                ICommand command => this.Equals(command),
                _ => false,
            };
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(
                this.Id,
                DateTimeOffset.MinValue,
                this.IsCompleted,
                this.Started);
        }
    }
}
