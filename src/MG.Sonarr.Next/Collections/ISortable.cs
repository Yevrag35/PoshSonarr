using System.Collections;

namespace MG.Sonarr.Next.Collections
{
    public interface ISortable
    {
        int Count { get; }
        void Sort();
    }
}
