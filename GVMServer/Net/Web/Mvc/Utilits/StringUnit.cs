namespace GVMServer.Net.Web.Mvc.Utilits
{
    public sealed class StringUnit
    {
        public static bool EqualsIgnoreCase(string x, string y)
        {
            int size = -1;
            int len = -1;
            if ((y == null && x == null) || ((len = y.Length) <= 0 & (size = x.Length) <= 0))
                return true;
            int i = 0;
            int m = ('a' - 'A');
            for (; i < len; i++)
            {
                char a = y[i];
                char b = x[i];
                if (!(a == b || (a + m) == b || (b + m) == a))
                    return false;
            }
            return (len == size);
        }
    }
}
