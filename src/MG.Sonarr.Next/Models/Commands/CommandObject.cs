using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System.Management.Automation;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.Commands
{
    [SonarrObject]
    public sealed class CommandObject : IdSonarrObject<CommandObject>,
        ICommand,
        IEquatable<CommandObject>,
        IJsonOnSerializing,
        ISerializableNames<CommandObject>
    {
        const string BODY = "Body";
        const int CAPACITY = 16;
        const string COMPLETED = "completed";
        static readonly string _typeName = typeof(CommandObject).GetTypeName();

        PSObject? _body;

        public DateTimeOffset? Ended { get; private set; }
        public bool IsCompleted => COMPLETED.Equals(this.Status, StringComparison.InvariantCultureIgnoreCase);
        public string Name { get; private set; } = string.Empty;
        public DateTimeOffset Started { get; private set; }
        public string Status { get; private set; } = string.Empty;

        public CommandObject()
            : base(CAPACITY)
        {
        }

        public static readonly ICommand Empty = EmptyCommand.Default;

        private static bool AreCommandPropertiesEqual(ICommand thisAsCommand, [NotNullWhen(true)] ICommand? otherCommand)
        {
            return thisAsCommand.Id.IsEqualTo(otherCommand?.Id)
                   &&
                   // Safe to access directly because other cannot be null if the first part is true.
                   thisAsCommand.Started == otherCommand.Started
                   &&
                   StringComparer.InvariantCultureIgnoreCase.Equals(thisAsCommand.Name, otherCommand.Name);
        }
        private static bool AreCommandPropertiesEqual(CommandObject @this, CommandObject? other)
        {
            return AreCommandPropertiesEqual(thisAsCommand: @this, otherCommand: other)
                   &&
                   // Safe to access directly because other cannot be null if the first part is true.
                   @this.Ended == other.Ended
                   &&
                   StringComparer.InvariantCultureIgnoreCase.Equals(@this.Status, other.Status);
        }
        public override void Commit()
        {
            this.Properties.Remove(BODY);
            base.Commit();
        }
        public int CompareTo(ICommand? other)
        {
            return Comparer<int?>.Default.Compare(this.Id, other?.Id);
        }
        public bool Equals([NotNullWhen(true)] CommandObject? other)
        {
            return ReferenceEquals(this, other) || AreCommandPropertiesEqual(this, other);
        }
        public bool Equals([NotNullWhen(true)] ICommand? command)
        {
            return ReferenceEquals(this, command) || AreCommandPropertiesEqual(this, command);
        }
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj switch
            {
                CommandObject co => this.Equals(co),
                ICommand command => this.Equals(command),
                _ => false,
            };
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(
                this.Id,
                this.Ended.GetValueOrDefault(),
                StringComparer.InvariantCultureIgnoreCase.GetHashCode(this.Name),
                this.Started,
                StringComparer.InvariantCultureIgnoreCase.GetHashCode(this.Status));
        }
        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.COMMAND];
        }
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
        public void OnSerializing()
        {
            this.AddProperty(BODY, _body);
        }
        public override void Reset()
        {
            this.Properties.Remove(BODY);
            base.Reset();
        }
        protected override void SetPSTypeName()
        {
            base.SetPSTypeName();
            this.TypeNames.Insert(0, _typeName);
        }

        static readonly HashSet<string> _capitalProps = new()
        {
            "Priority", "Status", "Trigger",
        };
        public static IReadOnlySet<string> GetPropertiesToCapitalize()
        {
            return _capitalProps;
        }

        public static bool operator ==(CommandObject? x, CommandObject? y)
        {
            return x.IsEqualTo<CommandObject>(y);
        }
        public static bool operator !=(CommandObject? x, CommandObject? y)
        {
            return !(x == y);
        }
        public static bool operator ==(CommandObject? x, ICommand? y)
        {
            return x.IsEqualTo(y);
        }
        public static bool operator !=(CommandObject? x, ICommand? y)
        {
            return !(x == y);
        }
        public static bool operator ==(ICommand? x, CommandObject? y)
        {
            return x.IsEqualTo(y);
        }
        public static bool operator !=(ICommand? x, CommandObject? y)
        {
            return !(x == y);
        }
    }
}
