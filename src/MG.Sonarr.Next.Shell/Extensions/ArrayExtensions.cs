using MG.Sonarr.Next.Shell.Components;

namespace MG.Sonarr.Next.Shell.Extensions
{
    internal static class ArrayExtensions
    {
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
