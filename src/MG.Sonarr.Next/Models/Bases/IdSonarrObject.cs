using MG.Sonarr.Next.Extensions.PSO;

namespace MG.Sonarr.Next.Models
{
    public abstract class IdSonarrObject<TSelf> : SonarrObject,
        IComparable<TSelf>,
        IHasId
        where TSelf : IdSonarrObject<TSelf>
    {
        public virtual int Id
        {
            get => this.GetValue<int>();
        }
        protected IdSonarrObject(int capacity)
            : base(capacity)
        {
        }

        public virtual int CompareTo(TSelf? other)
        {
            return Comparer<int?>.Default.Compare(this.Id, other?.Id);
        }
        public override void OnDeserialized()
        {

        }
    }
}
