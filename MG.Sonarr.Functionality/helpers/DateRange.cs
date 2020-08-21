using MG.Sonarr.Functionality.Url;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality.Helpers
{
    public class DateRange
    {
        internal const string Calendar_DTFormat = "yyyy-MM-ddTHH:mm:ss";
        private const string START_VERB = "Start - {0}";
        private const string END_VERB = "End   - {0}";
        private const string SHORT_FORMAT = "{0} {1}";
        private const string START = "start";
        private const string END = "end";

        private DateTime _start;
        private DateTime _end;

        public DateRange()
        {
            _start = DateTime.Now;
            _end = _start.AddDays(7);
        }
        public DateRange(DateTime start)
        {
            _start = start;
            _end = start.AddDays(7);
        }
        public DateRange(DateTime start, DateTime end)
        {
            _start = start;
            _end = end;
        }

        public IUrlParameter[] AsUrlParameters()
        {
            return new IUrlParameter[2]
            {
                new UrlParameter(START, _start.ToString(Calendar_DTFormat)),
                new UrlParameter(END, _end.ToString(Calendar_DTFormat))
            };
        }
        public string[] GetVerboseMessage()
        {
            string startForm = string.Format(SHORT_FORMAT, _start.ToShortDateString(), _start.ToShortTimeString());
            string endForm = string.Format(SHORT_FORMAT, _end.ToShortDateString(), _end.ToShortTimeString());

            string[] outMsg = new string[2]
            {
                string.Format(START_VERB, startForm),
                string.Format(END_VERB, endForm)
            };
            return outMsg;
        }

        public static DateRange NextWeek => SetNextWeekRange();
        public static DateRange ThisWeek => SetThisWeekRange();
        public static DateRange Today => SetOneDayRange(DateTime.Today);
        public static DateRange Tomorrow => SetOneDayRange(DateTime.Today.AddDays(1));
        public static DateRange Yesterday => SetOneDayRange(DateTime.Today.AddDays(-1));
        private static DateRange SetOneDayRange(DateTime beginning)
        {
            return new DateRange(beginning, beginning.AddDays(1).AddSeconds(-1));
        }
        private static DateRange SetThisWeekRange()
        {
            DateTime today = DateTime.Today;
            int begin = ((int)today.DayOfWeek - 1) * -1;
            DateTime start = today.AddDays(begin);
            return new DateRange(start, start.AddDays(7).AddSeconds(-1));
        }
        private static DateRange SetNextWeekRange()
        {
            DateRange range = SetThisWeekRange();
            range._start.AddDays(7);
            range._end.AddDays(7);
            return range;
        }
    }
}
