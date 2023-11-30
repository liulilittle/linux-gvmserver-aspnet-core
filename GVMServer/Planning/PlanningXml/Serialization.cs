namespace GVMServer.Planning.PlanningXml
{
    using System;
    using System.Collections.Generic;

    delegate object HandleXmlValue(string origin);
    /// <summary>
    /// 序列化一次处理过程，方便批量处理
    /// </summary>
    interface ISerializable
    {
        void SerializeData(ref List<HandleXmlValue> serializationHandlers);
    }

    /// <summary>
    /// const parsers
    /// </summary>
    static class SerializationHandlers
    {
        public static HandleXmlValue GetValueParser(Type Type_)
        {
            HandleXmlValue handler;
            if (Type_ == typeof(byte))
            {
                handler = SerializationHandlers.Int8Parser;
            }
            else if (Type_ == typeof(UInt16))
            {
                handler = SerializationHandlers.UInt16Parser;
            }
            else if (Type_ == typeof(UInt32))
            {
                handler = SerializationHandlers.UInt32Parser;
            }
            else if (Type_ == typeof(UInt64))
            {
                handler = SerializationHandlers.UInt64Parser;
            }
            else if (Type_ == typeof(Int16))
            {
                handler = SerializationHandlers.Int16Parser;
            }
            else if (Type_ == typeof(Int32))
            {
                handler = SerializationHandlers.Int32Parser;
            }
            else if (Type_ == typeof(Int64))
            {
                handler = SerializationHandlers.Int64Parser;
            }
            else if (Type_ == typeof(string))
            {
                handler = SerializationHandlers.StringParser;
            }
            else
            {
                handler = SerializationHandlers.UnSupportParser;
            }

            return handler;
        }

        public static HandleXmlValue Int8Parser = (string origin) =>
        {
            byte ret;
            if (byte.TryParse(origin, out ret))
                return ret;
            return 0;
        };

        public static HandleXmlValue Int16Parser = (string origin) =>
        {
            Int16 ret;
            if (Int16.TryParse(origin, out ret))
                return ret;
            return 0;
        };

        public static HandleXmlValue Int32Parser = (string origin) =>
        {
            Int32 ret;
            if (Int32.TryParse(origin, out ret))
                return ret;
            return 0;
        };

        public static HandleXmlValue Int64Parser = (string origin) =>
        {
            Int64 ret;
            if (Int64.TryParse(origin, out ret))
                return ret;
            return 0;
        };

        public static HandleXmlValue UInt8Parser = (string origin) =>
        {
            byte ret;
            if (byte.TryParse(origin, out ret))
                return ret;
            return 0;
        };

        public static HandleXmlValue UInt16Parser = (string origin) =>
        {
            UInt16 ret;
            if (UInt16.TryParse(origin, out ret))
                return ret;
            return 0;
        };

        public static HandleXmlValue UInt32Parser = (string origin) =>
        {
            UInt32 ret;
            if (UInt32.TryParse(origin, out ret))
                return ret;
            return 0;
        };

        public static HandleXmlValue UInt64Parser = (string origin) =>
        {
            UInt64 ret;
            if (UInt64.TryParse(origin, out ret))
                return ret;
            return 0;
        };

        public static HandleXmlValue StringParser = (string origin) =>
        {
            return origin;
        };

        public static HandleXmlValue UnSupportParser = (string origin) =>
        {
            Debugger.LogErr("Un Supported data type");
            return 0;
        };
    }
    
}
