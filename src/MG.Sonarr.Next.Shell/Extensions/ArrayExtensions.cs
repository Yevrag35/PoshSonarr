using MG.Sonarr.Next.Shell.Components;

namespace MG.Sonarr.Next.Shell.Extensions
{
    internal static class ArrayExtensions
    {
        internal static void UnionWith(this HashSet<Wildcard> set, string[]? array)
        {
            if (array is null)
            {
                return;
            }

            foreach (string str in array)
            {
                _ = set.Add(str);
            }
        }

        internal static IEnumerable<Wildcard> ToWildcards(this string[]? array)
        {
            if (array is null)
            {
                yield break;
            }

            foreach (string str in array)
            {
                yield return str;
            }
        }

        internal static void SplitToSets(this IntOrString[]? array, ISet<int> numbers, ISet<string> strings)
        {
            ArgumentNullException.ThrowIfNull(numbers);
            ArgumentNullException.ThrowIfNull(strings);

            if (array is null)
            {
                return;
            }

            foreach (IntOrString item in array)
            {
                if (item.IsNumber)
                {
                    numbers.Add(item.AsInt);
                }
                else if (item.IsString)
                {
                    strings.Add(item.AsString);
                }
            }
        }
        internal static void SplitToSets(this IntOrString[]? array, ISet<int> numbers, ISet<Wildcard> strings, bool explicitlyCalledForString = false)
        {
            ArgumentNullException.ThrowIfNull(numbers);
            ArgumentNullException.ThrowIfNull(strings);

            if (array is null)
            {
                return;
            }

            foreach (IntOrString item in array)
            {
                if (item.IsNumber && !explicitlyCalledForString)
                {
                    numbers.Add(item.AsInt);
                }
                else if (item.IsString)
                {
                    strings.Add(item.AsString);
                }
                else if (explicitlyCalledForString)
                {
                    strings.Add(item.AsInt.ToString());
                }
            }
        }
    }
}
