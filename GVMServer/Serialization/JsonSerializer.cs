namespace GVMServer.Serialization
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// JSON序列化与反序列
    /// </summary>
    public static partial class JsonSerializer
    {
        /// <summary>
        /// 序列化对象到JSON文本字符串
        /// </summary>
        /// <param name="o">欲被序列化的对象</param>
        /// <returns></returns>
        public static string Serializable(object o)
        {
            return JsonSerializer.Serializable(o, Formatting.None, "yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 序列化对象到JSON文本字符串
        /// </summary>
        /// <param name="o">欲被序列化的对象</param>
        /// <param name="formatting">序列化格式化设置，如果需要缩减体积建议设置None</param>
        /// <returns></returns>
        public static string Serializable(object o, Formatting formatting)
        {
            return JsonSerializer.Serializable(o, formatting, "yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 序列化对象到JSON文本字符串
        /// </summary>
        /// <param name="o">欲被序列化的对象</param>
        /// <param name="formatting">序列化对象过程中字符串格式设置，如果需要缩减体积建议设置None</param>
        /// <param name="dateformat">序列化对象过程中对日期时间的格式设置</param>
        /// <returns></returns>
        public static string Serializable(object o, Formatting formatting, string dateformat)
        {
            return JsonSerializer.Serializable(o, formatting, dateformat, NullValueHandling.Ignore);
        }

        /// <summary>
        /// 序列化对象到JSON文本字符串
        /// </summary>
        /// <param name="o">欲被序列化的对象</param>
        /// <param name="formatting">序列化对象过程中字符串格式设置，如果需要缩减体积建议设置None</param>
        /// <param name="dateformat">序列化对象过程中对日期时间的格式设置</param>
        /// <param name="nullValueHanding">序列化对象过程中是否忽略空值</param>
        /// <returns></returns>
        public static string Serializable(object o, Formatting formatting, string dateformat, NullValueHandling nullValueHanding)
        {
            IsoDateTimeConverter converter = new IsoDateTimeConverter()
            {
                DateTimeFormat = dateformat
            };
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
            settings.Converters.Add(converter);
            settings.NullValueHandling = nullValueHanding;
            return JsonConvert.SerializeObject(o, formatting, settings);
        }

        /// <summary>
        /// 反序列化JSON文本到对象，返回JToken它可能是JArray或者JObject请视具体情况操作
        /// </summary>
        /// <param name="value">欲被反序列化的JSON文本</param>
        /// <returns></returns>
        public static object Deserialize(string value)
        {
            return JsonConvert.DeserializeObject(value);
        }

        /// <summary>
        /// 反序列化JSON文本到指定类型的对象
        /// </summary>
        /// <param name="value">欲被反序列化的JSON文本</param>
        /// <param name="type">欲被反序列化到的对象类型</param>
        /// <returns></returns>
        public static object Deserialize(string value, Type type)
        {
            if (type == null)
            {
                return null;
            }
            return JsonConvert.DeserializeObject(value, type);
        }
        /// <summary>
        /// 反序列化JSON文本到泛型模板标识的类型对象
        /// </summary>
        /// <typeparam name="T">欲被反序列化的对象类型</typeparam>
        /// <param name="buffer">欲被反序列化的JSON文本</param>
        /// <returns></returns>
        public static T Deserialize<T>(string buffer)
        {
            return JsonConvert.DeserializeObject<T>(buffer);
        }

        /// <summary>
        /// 反序列化JObject对象到泛型模板标识的类型对象
        /// </summary>
        /// <typeparam name="T">欲被反序列化的对象类型</typeparam>
        /// <param name="value">欲被反序列化的JObject对象</param>
        /// <returns></returns>
        public static T Deserialize<T>(JObject value)
        {
            return value.ToObject<T>();
        }

        /// <summary>
        /// 反序列化JArray对象到泛型模板标识的类型对象
        /// </summary>
        /// <typeparam name="T">欲被反序列化的对象类型</typeparam>
        /// <param name="value">欲被反序列化的JObject对象</param>
        /// <returns></returns>
        public static IList<T> Deserialize<T>(JArray value)
        {
            IList<T> buffer = new List<T>(value.Count);
            for (int i = 0; i < value.Count; i++)
            {
                JObject o = (JObject)value[i];
                if (o != null)
                {
                    buffer[i] = o.ToObject<T>();
                }
            }
            return buffer;
        }

        /// <summary>
        /// 反序列化NameValueCollection字典集合对象到泛型模板标识的类型对象，如“Deserialize<Car>(Request.Form);”
        /// </summary>
        /// <typeparam name="T">欲被反序列化的对象类型</typeparam>
        /// <param name="value">反序列化NameValueCollection字典集合对象到</param>
        /// <returns></returns>
        public static T Deserialize<T>(NameValueCollection collection)
        {
            object obj = JsonSerializer.Deserialize(collection, typeof(T));
            if (obj == null)
            {
                return default(T);
            }
            return (T)obj;
        }

        public static object Deserialize(NameValueCollection collection, Type type)
        {
            if (collection == null || type == null)
            {
                return null;
            }
            JObject message = new JObject();
            for (int i = 0; i < collection.Count; i++)
            {
                string key = collection.Keys[i];
                message[key] = collection[i];
            }
            foreach (PropertyInfo prop in type.GetProperties())
            {
                JToken token = message[prop.Name];
                if (token != null && token.Type == JTokenType.String)
                {
                    JValue key = (JValue)token;
                    if (key.Value == null)
                    {
                        continue;
                    }
                    Type contravariant = prop.PropertyType;
                    if (contravariant == typeof(bool))
                    {
                        object value = key.Value;
                        if (value == null)
                            key.Value = false;
                        if (("1").Equals(value))
                            key.Value = true;
                        if (("0").Equals(value))
                            key.Value = false;
                    }
                    if (!typeof(ValueType).IsAssignableFrom(contravariant) && contravariant != typeof(string))
                    {
                        try
                        {
                            message[prop.Name] = (JToken)JsonConvert.DeserializeObject((string)key.Value);
                        }
                        catch { }
                    }
                    if (contravariant.IsGenericType)
                    {
                        Type covariant = (contravariant.GetGenericTypeDefinition());
                        if (typeof(Nullable<>) == covariant) // 确认是从可空值类型协助变换派生的类型
                        {
                            if (contravariant.BaseType == typeof(ValueType))
                            {
                                if (RuntimeHelpers.Equals(key.Value, string.Empty))
                                    key.Value = null;
                            }
                        }
                    }
                }
            }
            return message.ToObject(type);
        }
    }
}
