using MG.Sonarr.Next.Extensions;
using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties
{
    public sealed class ReadOnlySetProperty<T> : ReadOnlyProperty<SortedSet<T>>
    {
        const string AS_STRING = "read-only SortedSet{0} {1}=";
        const string AS_VALUES = AS_STRING + "{{{2}}}";
        const string AS_EMPTY = AS_STRING + "{{}}";
        readonly string _genName;

        public SortedSet<T> SetValues { get; internal set; }
        protected override SortedSet<T> ValueAsT => this.SetValues;

        public ReadOnlySetProperty(string propertyName, SortedSet<T> set)
            : base(propertyName)
        {
            this.SetValues = set;
            _genName = typeof(T).GetPSTypeName();
        }

        public override PSMemberInfo Copy()
        {
            return this;
        }

        public override string ToString()
        {
            if (this.SetValues.Count <= 0)
            {
                return string.Format(AS_EMPTY, _genName, this.Name);
            }

            return string.Format(AS_VALUES, _genName, this.Name, string.Join(',', this.SetValues));
        }
    }
}

