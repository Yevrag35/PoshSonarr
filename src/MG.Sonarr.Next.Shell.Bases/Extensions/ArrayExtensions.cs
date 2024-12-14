using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Extensions
{
    public static class ArrayExtensions
    {

        //public static void SplitToSets(this IntOrString[]? array, ISet<int> numbers, ISet<string> strings)
        //{
        //    ArgumentNullException.ThrowIfNull(numbers);
        //    ArgumentNullException.ThrowIfNull(strings);

        //    if (array is null)
        //    {
        //        return;
        //    }

        //    foreach (IntOrString item in array)
        //    {
        //        if (item.IsNumber)
        //        {
        //            numbers.Add(item.AsInt);
        //        }
        //        else if (item.IsString)
        //        {
        //            strings.Add(item.AsString);
        //        }
        //    }
        //}
        //public static void SplitToSets(this IntOrString[]? array, ISet<int> numbers, ISet<Wildcard> strings, bool explicitlyCalledForString = false)
        //{
        //    ArgumentNullException.ThrowIfNull(numbers);
        //    ArgumentNullException.ThrowIfNull(strings);

        //    if (array is null)
        //    {
        //        return;
        //    }

        //    foreach (IntOrString item in array)
        //    {
        //        if (item.IsNumber && !explicitlyCalledForString)
        //        {
        //            numbers.Add(item.AsInt);
        //        }
        //        else if (item.IsString)
        //        {
        //            strings.Add(item.AsString);
        //        }
        //        else if (explicitlyCalledForString)
        //        {
        //            strings.Add(item.AsInt.ToString());
        //        }
        //    }
        //}
        public static void SplitToSets(this Either<string, int>[]? array, ISet<int> numbers, WildcardSet strings, bool explicitlyCalledForString = false)
        {
            ArgumentNullException.ThrowIfNull(numbers);
            ArgumentNullException.ThrowIfNull(strings);

            if (array is null)
            {
                return;
            }

            foreach (Either<string, int> item in array)
            {
                switch (item.Index)
                {
                    case 1u:
                        strings.Add(item.AsT1!);
                        break;

                    case 2u when !explicitlyCalledForString:
                        numbers.Add(item.AsT2);
                        break;

                    case 2u when explicitlyCalledForString:
                        strings.Add(item.AsT2.ToString());
                        break;

                    default:
                        numbers.Add(item.AsT2);
                        break;
                }
            }
        }
    }
}
