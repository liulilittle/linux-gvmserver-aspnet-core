namespace GVMServer.Utilities
{
    using System;

    public static class DateTimes
    {
        public static long ToTimespan10(this DateTime dateTime)
        {
            return ToTimespan13(dateTime) / 1000;
        }

        public static long ToTimespan13(this DateTime dateTime)
        {
            TimeSpan ts = dateTime.Subtract(TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1), TimeZoneInfo.Local));
            return ts.Ticks / 10000;
        }

        public static DateTime FromTimespan10(this long timestamp)
        {
            return FromTimespan13(timestamp * 1000);
        }

        public static DateTime FromTimespan13(this long timestamp)
        {
            DateTime ts = TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1), TimeZoneInfo.Local);
            ts = ts.AddMilliseconds(timestamp);
            return ts;
        }
    }
}
