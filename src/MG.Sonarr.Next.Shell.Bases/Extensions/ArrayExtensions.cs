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
                    case 1u when !explicitlyCalledForString && int.TryParse(item.AsT1, out int number):
                        numbers.Add(number);
                        break;

                    case 1u:
                        strings.Add(item.AsT1);
                        break;

                    case 2u when explicitlyCalledForString:
                        AddSpan(strings, item.AsT2);
                        break;

                    case 2u:
                        numbers.Add(item.AsT2);
                        break;

                    default:
                        goto case 2u;
                }
            }
        }

        private static void AddSpan(WildcardSet set, int value)
        {
#if NET9_0_OR_GREATER
            Span<char> chars = stackalloc char[LengthConstants.INT_MAX];
            _ = value.TryFormat(chars, out int written);
            chars = chars.Slice(0, written);
            set.Add(chars);
#else
            set.Add(value.ToString());
#endif
        }
    }
}
