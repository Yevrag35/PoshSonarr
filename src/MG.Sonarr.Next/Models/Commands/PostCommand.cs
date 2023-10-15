namespace MG.Sonarr.Next.Models.Commands
{
    public sealed class PostCommand
    {
        public required string CommandName { get; init; }
        public required string Name { get; init; }
        public CommandPriority Priority { get; init; } = CommandPriority.Normal;
        public bool UpdateScheduledTask { get; init; } = true;
    }
}
