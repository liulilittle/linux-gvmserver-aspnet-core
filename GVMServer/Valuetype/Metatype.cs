namespace GVMServer.Utilities
{
    using System;
    using System.Collections.Generic;

    public class Metatype
    {
        public static Type GetArrayElement(Type array)
        {
            if (array.IsArray)
            {
                return array.GetElementType();
            }
            if (Metatype.IsList(array))
            {
                Type[] args = array.GetGenericArguments();
                return args[0];
            }
            return null;
        }
        public static bool IsBuffer(Type clazz)
        {
            return clazz == typeof(byte[]);
        }
        public static bool IsString(Type clazz)
        {
            return clazz == typeof(string);
        }
        public static bool IsList(Type clazz)
        {
            if (clazz == null)
                return false;
            if (clazz.IsArray)
                return true;
            return clazz.IsGenericType && (typeof(IList<>).GUID == clazz.GUID || typeof(List<>).GUID == clazz.GUID);
        }
    }
}
