using System.Management.Automation;

namespace MG.Sonarr.Next.Services.Extensions.PSO
{
    public static class PSMemberInfoCollectionExtensions
    {
        public static void RemoveAll<T>(this PSMemberInfoCollection<T> collection, Func<T, bool> predicate)
            where T : PSMemberInfo
        {
            foreach (T item in collection)
            {
                if (predicate(item))
                {
                    collection.Remove(item.Name);
                }
            }
        }

        public static void RemoveMany<T>(this PSMemberInfoCollection<T> collection, params string[] propertyNames) where T : PSMemberInfo
        {
            if (propertyNames is null || propertyNames.Length <= 0)
            {
                return;
            }

            foreach (string name in propertyNames)
            {
                collection.Remove(name);
            }
        }
    }
}
