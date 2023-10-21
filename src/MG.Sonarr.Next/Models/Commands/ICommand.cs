﻿namespace MG.Sonarr.Next.Models.Commands
{
    public interface ICommand : IComparable<ICommand>, IEquatable<ICommand>
    {
        int Id { get; }
        [MemberNotNullWhen(true, nameof(Ended))]
        bool IsCompleted { get; }
        string Name { get; }
        DateTimeOffset Started { get; }
        DateTimeOffset? Ended { get; }
    }
}
