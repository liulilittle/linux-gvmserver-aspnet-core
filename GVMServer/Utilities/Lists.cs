namespace GVMServer.Utilities
{
    using System.Collections;
    using System.Collections.Generic;

    public static class Lists
    {
        public static T Index<T>(this IList<T> o, int index)
        {
            if (o == null || index < 0 || index >= o.Count)
            {
                return default(T);
            }
            return o[index];
        }

        public static T Index<T>(this IList<T> o, int index, T value)
        {
            if (o == null || index < 0 || index >= o.Count)
            {
                return default(T);
            }
            T r = o[index];
            o[index] = value;
            return r;
        }

        public static bool IsNullOrEmpty(this IList value)
        {
            return (value == null || value.Count <= 0);
        }

        public static bool IsNullOrEmpty<T>(this IList<T> value)
        {
            return (value == null || value.Count <= 0);
        }

        public static IList<string> Transform(this string value, params char[] separator)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            if (separator == null || separator.Length <= 0)
            {
                return new string[] { value };
            }
            return value.Split(separator: separator);
        }
    }
}
