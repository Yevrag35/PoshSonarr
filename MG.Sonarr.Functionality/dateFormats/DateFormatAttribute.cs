using System;
using System.Collections.Generic;
using System.Reflection;

namespace MG.Sonarr.Functionality.DateFormats
{
    public class DateFormatAttribute : Attribute
    {
        public string Value { get; }
        public DateFormatAttribute(string dateFormat) => this.Value = dateFormat;

        public static bool TryGetFormatFromString<T>(string possibility, out T outEnum) where T : Enum
        {
            outEnum = default;
            bool result = false;
            foreach ((string, T) tup in GetAllFormats<T>())
            {
                if (possibility.Equals(tup.Item1, StringComparison.InvariantCultureIgnoreCase))
                {
                    outEnum = tup.Item2;
                    result = true;
                    break;
                }
            }
            return result;
        }
        public static string GetStringFromFormat<T>(T dateFormat) where T : Enum
        {
            return dateFormat.GetType().GetField(dateFormat.ToString()).GetCustomAttribute<DateFormatAttribute>().Value;
        }
        private static IEnumerable<(string, T)> GetAllFormats<T>() where T : Enum
        {
            Type type = typeof(T);
            foreach (T e in Enum.GetValues(type))
            {
                yield return (type.GetField(e.ToString()).GetCustomAttribute<DateFormatAttribute>().Value, e);
            }
        }
    }
}
