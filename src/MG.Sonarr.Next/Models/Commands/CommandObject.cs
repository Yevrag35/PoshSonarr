using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Models.Commands
{
    public sealed class CommandObject : SonarrObject,
        ICommand,
        IComparable<CommandObject>,
        IJsonOnSerializing,
        ISerializableNames<CommandObject>
    {
        const string BODY = "Body";
        const int CAPACITY = 16;
        const string COMPLETED = "completed";

        PSObject? _body;

        public int Id { get; private set; }
        public bool IsCompleted => COMPLETED.Equals(this.Status, StringComparison.InvariantCultureIgnoreCase);
        public string Name { get; private set; } = string.Empty;
        public DateTimeOffset Started { get; private set; }
        public DateTimeOffset? Ended { get; private set; }
        public string Status { get; private set; } = string.Empty;

        public CommandObject()
            : base(CAPACITY)
        {
        }

        public int CompareTo(CommandObject? other)
        {
            return Comparer<int?>.Default.Compare(this.Id, other?.Id);
        }
        public static readonly ICommand Empty = EmptyCommand.Default;
        protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
        {
            return resolver[Meta.COMMAND];
        }

        public override void OnDeserialized()
        {
            base.OnDeserialized();
            if (this.TryGetProperty(BODY, out PSObject? body))
            {
                _body = body;
                this.Properties.Remove(BODY);
            }

            if (this.TryGetId(out int id))
            {
                this.Id = id;
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

        public override void Commit()
        {
            this.Properties.Remove(BODY);
            base.Commit();
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
    }
}
