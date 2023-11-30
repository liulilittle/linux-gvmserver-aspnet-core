namespace GVMServer.Net.Web.Mvc.Utilits
{
    using System;
    using System.Collections.Generic;

    public static class UriResolveUnit
    {
        public static IList<string> Segments(string path)
        {
            if (path == null || path.Length <= 0)
                throw new ArgumentException();
            IList<string> segment = new List<string>();
            int len = path.Length;
            int i = 0;
            int j = 0;
            for (; i < len; i++)
            {
                char asc = path[i];
                if ((asc == '/' || asc == '\\'))
                {
                    int size = (i - j);
                    if (size > 0)
                        segment.Add(path.Substring(j, size));
                    j = i + 1;
                }
            }
            if (j < len)
            {
                int size = (len - j);
                if (size > 0)
                    segment.Add(path.Substring(j, size));
            }
            return segment;
        }

        public static bool Equals(string map, string raw)
        {
            int size = -1;
            int len = -1; 
            if ((raw == null && map == null) || ((len = raw.Length) <= 0 & (size = map.Length) <= 0))
                return true;
            int i = 0;
            int m = ('a' - 'A');
            if (len != size)
                return false;
            for (; i < len; i++)
            {
                char a = raw[i];
                if (i >= size && (a == '/' || a == '\\'))
                    return true;
                char b = map[i];
                if (!(a == b || (a + m) == b || (b + m) == a))
                    return false;
            }
            return (len == size);
        }
    }
}
