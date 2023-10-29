using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties
{
    public sealed class ReadOnlyPSObjectProperty<T> : ReadOnlyProperty<T> where T : PSObject
    {
        protected override T ValueAsT { get; }

        public ReadOnlyPSObjectProperty(string propertyName, T pso)
            : base(propertyName)
        {
            this.ValueAsT = pso;
        }

        public override PSMemberInfo Copy()
        {
            return new ReadOnlyPSObjectProperty<T>(this.Name, (T)this.ValueAsT.Copy());
        }
    }
}

