namespace GVMServer.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public static class Attributes
    {
        public static IList<T> GetAttributes<T>(MemberInfo member)
            where T : Attribute
        {
            if (member == null)
            {
                throw new ArgumentNullException();
            }
            object[] attris = member.GetCustomAttributes(typeof(T), true);
            if (Lists.IsNullOrEmpty<object>(attris))
            {
                return null;
            }
            IList<T> attributes = new List<T>();
            foreach (object attri in attris)
            {
                if (attri is T)
                {
                    attributes.Add((T)attri);
                }
            }
            return attributes;
        }

        public static T GetAttribute<T>(MemberInfo member) where T : Attribute
        {
            Attribute attr = Attributes.GetAttribute(member, typeof(T));
            if (attr == null)
            {
                return null;
            }
            return attr as T;
        }

        public static Attribute GetAttribute(MemberInfo member, Type attribute)
        {
            if (attribute == null || member == null)
            {
                throw new ArgumentNullException();
            }
            if (!typeof(Attribute).IsAssignableFrom(attribute))
            {
                return null;
            }
            object[] attris = member.GetCustomAttributes(attribute, true);
            if (Lists.IsNullOrEmpty<object>(attris))
            {
                return null;
            }
            foreach (object o in attris)
            {
                if (attribute.IsInstanceOfType(o))
                {
                    return (Attribute)o;
                }
            }
            return null;
        }
    }
}
