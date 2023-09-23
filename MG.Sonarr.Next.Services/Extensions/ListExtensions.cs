using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Services.Extensions
{
    public static class ListExtensions
    {
        public static void AddMany<T>(this IList<T> list, params T[] values)
        {
            AddMany(list, collection: values);
        }

        public static void AddMany<T>(this IList<T> list, IEnumerable<T> collection)
        {
            if (collection is null)
            {
                return;
            }

            foreach (T value in collection)
            {
                list.Add(value);
            }
        }
    }
}
