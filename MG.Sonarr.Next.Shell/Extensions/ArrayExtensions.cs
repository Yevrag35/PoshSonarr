using MG.Sonarr.Next.Shell.Components;

namespace MG.Sonarr.Next.Shell.Extensions
{
    public static class ArrayExtensions
    {
        public static void SplitToSets(this IntOrString[]? array, ISet<int> numbers, ISet<string> strings)
        {
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
        public static void SplitToSets(this IntOrString[]? array, ISet<int> numbers, ISet<WildcardString> strings)
        {
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
    }
}
