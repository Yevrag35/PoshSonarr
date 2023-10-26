using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties
{
    public sealed class ReadOnlyPSObjectProperty : ReadOnlyProperty<PSObject>
    {
        protected override PSObject ValueAsT { get; }

        public ReadOnlyPSObjectProperty(string propertyName, PSObject pso)
            : base(propertyName)
        {
            this.ValueAsT = pso;
        }

        public override PSMemberInfo Copy()
        {
            return new ReadOnlyPSObjectProperty(this.Name, this.ValueAsT.Copy());
        }
    }
}

