namespace GVMServer.W3Xiyou.Docking
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using GVMServer.Serialization;
    using GVMServer.Utilities;
    using JsonPropertyAttribute = Newtonsoft.Json.JsonPropertyAttribute;

    public static class XiYouSerializer
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly IDictionary<Type, IList<PropertyInfo>> m_SignTextOrder = new Dictionary<Type, IList<PropertyInfo>>();

        private static IList<PropertyInfo> GetAddSignTextOrder(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            lock (m_SignTextOrder)
            {
                if (!m_SignTextOrder.TryGetValue(type, out IList<PropertyInfo> properties))
                {
                    properties = type.GetProperties().OrderBy(i => i.Name, StringComparer.Ordinal).ToList();
                    m_SignTextOrder.TryAdd(type, properties);
                }
                return properties;
            }
        }

        public static string SerializableJson(object o)
        {
            return JsonSerializer.Serializable(o); ;
        }

        public static T DeserializeJson<T>(string s)
        {
            if (typeof(T) == typeof(string))
            {
                return (T)(object)s;
            }
            s = s.TrimEnd().TrimStart();
            if (string.IsNullOrEmpty(s))
            {
                return default(T);
            }
            return JsonSerializer.Deserialize<T>(s);
        }

        public static string FetchSignTextOrMessage(object o)
        {
            return FetchSignTextOrMessage(o, 0);
        }

        public static string FetchSignTextOrMessage(object o, int category, bool sign = false)
        {
            string contents = string.Empty;
            if (null != o)
            {
                foreach (PropertyInfo info in GetAddSignTextOrder(o.GetType()))
                {
                    string propertyName = Attributes.GetAttribute<JsonPropertyAttribute>(info)?.PropertyName ?? info.Name;
                    object oValue = info.GetValue(o) ?? string.Empty;
                    if (!(oValue is string))
                    {
                        oValue = SerializableObject(oValue);
                    }
                    string kvVal = oValue.ToString().TrimStart().TrimEnd(); // 删首尾空
                    if (sign)
                    {
                        if (null == Attributes.GetAttribute<SignIgnoreAttribute>(info))
                        {
                            contents += $"{ propertyName }={ kvVal }&";
                        }
                    }
                    else
                    {
                        contents += $"{ propertyName }={ XiYouUtility.UrlEncode(kvVal) }&";
                    }
                }
            }
            if (contents.Length != 0)
            {
                contents = contents.Remove(contents.Length - 1);
            }
            if (sign)
            {
                contents = XiYouUtility.SignText(contents, category);
            }
            return contents;
        }

        public static T DeserializeObject<T>(NameValueCollection s)
        {
            return DeserializeObject<T>(s, (Func<NameValueCollection, string, bool>)null);
        }

        public static T DeserializeObject<T>(NameValueCollection s, Func<NameValueCollection, string, bool> predicate)
        {
            if (s == null)
            {
                return default(T);
            }
            NameValueCollection collection = XiYouUtility.UrlDecode(s);
            if (predicate != null)
            {
                string signStr = string.Empty; // 欲加签的字符串
                foreach (PropertyInfo info in GetAddSignTextOrder(typeof(T)))
                {
                    if (Attributes.GetAttribute<SignIgnoreAttribute>(info) != null)
                    {
                        continue;
                    }
                    string propertyName = info.Name;
                    do
                    {
                        JsonPropertyAttribute property = Attributes.GetAttribute<JsonPropertyAttribute>(info);
                        if (property != null)
                        {
                            propertyName = property.PropertyName;
                        }
                    } while (false);
                    signStr += $"{ propertyName }={ collection.Get(propertyName) }&";
                }
                if (signStr.Length > 0)
                {
                    signStr = signStr.Remove(signStr.Length - 1);
                }
                if (!predicate(collection, signStr))
                {
                    return default(T);
                }
            }
            try
            {
                return JsonSerializer.Deserialize<T>(collection);
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        public static string SerializableObject(object o)
        {
            return SerializableJson(o);
        }
    }
}
