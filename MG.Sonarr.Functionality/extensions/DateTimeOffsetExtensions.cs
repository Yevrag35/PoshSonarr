using System;

namespace MG.Sonarr.Functionality.Extensions
{
    internal static class DateTimeOffsetExtensions
    {
        private const string TIMEZONE_ID = "Tokyo Standard Time";

        private static TimeSpan GetUtcOffset(DateTimeOffset offset)
        {
            TimeZoneInfo timeZone = GetTokyoTimeZone();
            return timeZone.GetUtcOffset(offset);
        }
        private static TimeZoneInfo GetTokyoTimeZone() => TimeZoneInfo.FindSystemTimeZoneById(TIMEZONE_ID);

        internal static DateTimeOffset ToAnimeTime(this DateTimeOffset offset)
        {
            TimeSpan difference = GetUtcOffset(offset);
            return offset.ToOffset(difference);
        }
    }
}
