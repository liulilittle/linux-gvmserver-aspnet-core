namespace GVMServer.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Serialization;

    public sealed partial class XmlSerializer
    {
        public string Serializable(object o)
        {
            if (o == null)
            {
                throw new ArgumentNullException();
            }
            return Serializable(o, o.GetType());
        }

        public string Serializable(object o, Type type)
        {
            if (o == null || type == null)
            {
                throw new ArgumentNullException();
            }
            return InternalSerializable(o, type);
        }

        public string Serializable<T>(object o)
        {
            return Serializable(o, typeof(T));
        }

        public object Deserialize(string buffer, Type type)
        {
            if (string.IsNullOrEmpty(buffer) || type == null)
            {
                throw new ArgumentNullException();
            }
            XmlDocument document = new XmlDocument();
            document.LoadXml(buffer);
            return Deserialize(document.DocumentElement, type);
        }

        public T Deserialize<T>(string buffer)
        {
            return (T)Deserialize(buffer, typeof(T));
        }

        public object Deserialize(XmlNode element, Type type)
        {
            if (element == null || type == null)
            {
                return element;
            }
            if (element.LocalName != "object")
            {
                throw new ArgumentException();
            }
            return InternalDeserialize(element, type);
        }

        public T Deserialize<T>(XmlNode element)
        {
            object o = Deserialize(element, typeof(T));
            if (o == null)
            {
                throw new ArgumentNullException();
            }
            if (!typeof(T).IsInstanceOfType(o))
            {
                throw new InvalidCastException();
            }
            return (T)o;
        }
    }

    public sealed partial class XmlSerializer
    {
        private static IList<Type> __slstBaseTypeTable = new List<Type>()
    {
  typeof(string),
  typeof(long),
  typeof(ulong),
  typeof(byte),
  typeof(sbyte),
  typeof(double),
  typeof(float),
  typeof(short),
  typeof(ushort),
  typeof(char),
  typeof(decimal),
  typeof(int),
  typeof(uint),
  typeof(DateTime),
      };
        private static IList<string> __slstBaseTypeNameTable = new List<string>()
    {
  "string",
  "long",
  "ulong",
  "byte",
  "sbyte",
  "double",
  "float",
  "short",
  "ushort",
  "char",
  "decimal",
  "int",
  "uint",
  "DateTime",
      };
    }

    public sealed partial class XmlSerializer
    {
        private T InternalGetCustomAttributes<T>(MemberInfo __objRefType) where T : Attribute
        {
            object[] __objRefAttr = __objRefType.GetCustomAttributes(typeof(T), false);
            if (__objRefAttr.Length <= 0)
            {
                return null;
            }
            return (T)__objRefAttr[0];
        }

        private string InternalGetXmlElementName(MemberInfo __objRefMember)
        {
            if (__objRefMember == null)
            {
                return __objRefMember.Name;
            }
            XmlElementAttribute __objRefAttr = InternalGetCustomAttributes<XmlElementAttribute>(__objRefMember);
            if (__objRefAttr == null)
            {
                return __objRefMember.Name;
            }
            return __objRefAttr.ElementName;
        }

        private string InternalGetXmlAttributeName(MemberInfo __objRefMember)
        {
            if (__objRefMember == null)
            {
                return null;
            }
            XmlAttributeAttribute __objRefAttr = InternalGetCustomAttributes<XmlAttributeAttribute>(__objRefMember);
            if (__objRefAttr == null)
            {
                return null;
            }
            return __objRefAttr.AttributeName;
        }

        private bool InternalHasXmlAttribute(MemberInfo __objRefMember)
        {
            if (__objRefMember == null)
            {
                return false;
            }
            XmlAttributeAttribute __objRefAttr = InternalGetCustomAttributes<XmlAttributeAttribute>(__objRefMember);
            if (__objRefAttr == null)
            {
                return false;
            }
            return true;
        }

        private bool InternalXmlIsIgnore(MemberInfo __objRefMember)
        {
            if (__objRefMember == null)
            {
                return false;
            }
            XmlIgnoreAttribute __objRefAttr = InternalGetCustomAttributes<XmlIgnoreAttribute>(__objRefMember);
            return __objRefAttr != null;
        }

        private Type InternalGetStandardType(Type __objRefType)
        {
            if (__objRefType.IsArray)
            {
                string __strFullName = __objRefType.FullName; __strFullName = __strFullName.Remove(__strFullName.Length - 2);
                Assembly __asm = Assembly.Load(__objRefType.Assembly.FullName);
                return __asm.GetType(__strFullName, true);
            }
            return __objRefType;
        }

        private string InternalSerializable(object __objRef, Type __objRefType)
        {
            XmlDocument _sXmlDocument = new XmlDocument();
            XmlNode _sXmlRootNode = _sXmlDocument.AppendChild(_sXmlDocument.CreateElement("object"));
            _sXmlRootNode.Attributes.Append(_sXmlDocument.CreateAttribute("class")).Value = InternalGetTypeQualifiedName(__objRef.GetType());
            _sXmlRootNode.Attributes.Append(_sXmlDocument.CreateAttribute("interface")).Value = InternalGetTypeQualifiedName(__objRefType);
            InternalTraversalSerializable(__objRef, __objRef.GetType(), _sXmlRootNode, _sXmlDocument);
            return _sXmlDocument.OuterXml;
        }

        private bool InternalXmlBindAttribute(XmlNode __objRefXmlRootNode, object __objRef, PropertyInfo _objRefProperty, XmlDocument _sXmlDocument)
        {
            string __strXmlAttrName = InternalGetXmlAttributeName(_objRefProperty);
            if (__strXmlAttrName == null)
            {
                return false;
            }
            XmlAttribute __objRefXmlAttr = _sXmlDocument.CreateAttribute(__strXmlAttrName);
            __objRefXmlAttr.Value = Convert.ToString(__objRef ?? string.Empty);
            try
            {
                return true;
            }
            finally
            {
                __objRefXmlRootNode.Attributes.Append(__objRefXmlAttr);
            }
        }

        private string InternalGetTypeQualifiedName(Type __objRefType)
        {
            if (__objRefType == null)
            {
                return string.Empty;
            }
            string buffer = __objRefType.AssemblyQualifiedName;
            Match match = Regex.Match(buffer, @"(.*?\,\s{0,1}.*?)\,");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return __objRefType.FullName;
        }

        private void InternalTraversalSerializable(object __objRef, Type __objRefType, XmlNode __objRefXmlRootNode, XmlDocument _sXmlDocument)
        {
            if (__objRef == null)
            {
                return;
            }
            foreach (PropertyInfo _sProperyNameIter in __objRefType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (InternalXmlIsIgnore(_sProperyNameIter))
                {
                    continue; // filtering does not want to be serialized value.
                }
                Type __objRefIterType = _sProperyNameIter.PropertyType;
                if (!_sProperyNameIter.DeclaringType.IsInstanceOfType(__objRef))
                {
                    continue;
                }
                object __objRefIterValue = _sProperyNameIter.GetValue(__objRef, null);
                if (__objRefIterValue != null && __objRefIterType == typeof(object))
                {
                    __objRefIterType = __objRefIterValue.GetType();
                }
                //XmlElement __objRefIterXmlElement = _sXmlDocument.CreateElement(InternalGetXmlElementName(_sProperyNameIter));
                XmlElement __objRefIterXmlElement = _sXmlDocument.CreateElement("property");
                __objRefIterXmlElement.SetAttributeNode(_sXmlDocument.CreateAttribute("name")).Value = InternalGetXmlElementName(_sProperyNameIter);
                if (__objRefIterValue != null && _sProperyNameIter.PropertyType == typeof(object))
                {
                    __objRefIterType = __objRefIterValue.GetType();
                    __objRefIterXmlElement.SetAttributeNode(_sXmlDocument.CreateAttribute("class")).Value = InternalGetTypeQualifiedName(__objRefIterType);
                }
                bool __bIsBaseType = __slstBaseTypeTable.Contains(__objRefIterType);
                if (__bIsBaseType && !InternalXmlBindAttribute(__objRefXmlRootNode, __objRefIterValue, _sProperyNameIter, _sXmlDocument))
                {
                    __objRefXmlRootNode.AppendChild(__objRefIterXmlElement);
                    __objRefIterXmlElement.InnerText = Convert.ToString(__objRefIterValue ?? string.Empty);
                }
                else if (__objRefIterType.IsArray)
                {
                    __objRefIterXmlElement.SetAttributeNode(_sXmlDocument.CreateAttribute("type")).Value = "array";
                    if (__objRefIterValue == null)
                    {
                        continue;
                    }
                    __objRefIterType = InternalGetStandardType(__objRefIterType);
                    if (__objRefIterType == typeof(object))
                    {
                        __objRefIterType = __objRefIterValue.GetType();
                        __objRefIterXmlElement.SetAttributeNode(_sXmlDocument.CreateAttribute("class")).Value =
                            InternalGetTypeQualifiedName(__objRefIterType);
                    }
                    Array __objRefArray = (Array)__objRefIterValue;
                    int __nOffsBaseType = __slstBaseTypeTable.IndexOf(__objRefIterType);
                    __objRefXmlRootNode.AppendChild(__objRefIterXmlElement);
                    Type __objRefArrType = __objRefIterType.GetElementType();
                    for (int i = 0; i < __objRefArray.Length; i++)
                    {
                        int __nOffsRealBaseType = __nOffsBaseType;
                        object __nRealArrObject = __objRefArray.GetValue(i);
                        if (__nRealArrObject != null)
                        {
                            __nOffsRealBaseType = __slstBaseTypeTable.IndexOf(__nRealArrObject.GetType());
                        }
                        if (__nOffsRealBaseType > -1)
                        {
                            XmlElement __objRefIterArrNode = _sXmlDocument.CreateElement(__slstBaseTypeNameTable[__nOffsRealBaseType]);
                            __objRefIterXmlElement.AppendChild(__objRefIterArrNode);
                            __objRefIterArrNode.InnerText = Convert.ToString(__nRealArrObject ?? string.Empty);
                        }
                        else
                        {
                            //XmlElement __objRefIterArrNode = _sXmlDocument.CreateElement(InternalGetXmlElementName(_sProperyNameIter));
                            Type __objRefRealIterType = __nRealArrObject.GetType();
                            XmlElement __objRefIterArrNode = _sXmlDocument.CreateElement("object");
                            if (__objRefArrType == typeof(object))
                            {
                                __objRefIterArrNode.SetAttributeNode(_sXmlDocument.CreateAttribute("class")).Value =
                                    InternalGetTypeQualifiedName(__objRefRealIterType);
                            }
                            __objRefIterXmlElement.AppendChild(__objRefIterArrNode);
                            InternalTraversalSerializable(__nRealArrObject, __objRefRealIterType, __objRefIterArrNode, _sXmlDocument);
                        }
                    }
                }
                else if (!__bIsBaseType)
                {
                    __objRefXmlRootNode.AppendChild(__objRefIterXmlElement);
                    InternalTraversalSerializable(__objRefIterValue, __objRefIterType, __objRefIterXmlElement, _sXmlDocument);
                }
            }
        }
    }

    public sealed partial class XmlSerializer
    {
        private object InternalDeserialize(XmlNode element, Type type)
        {
            if (element == null || type == null)
            {
                throw new ArgumentNullException();
            }
            if (type.IsInterface)
            {
                type = InternalGetTypeByElementAttribute(element);
            }
            if (type == null)
            {
                throw new ArgumentException();
            }
            object __objRef = Activator.CreateInstance(type);
            if (__objRef == null)
            {
                throw new ArgumentNullException();
            }
            try
            {
                return __objRef;
            }
            finally
            {
                InternalTraversalDeserialize(__objRef, type, element);
            }
        }

        private XmlElement InternalGetElementByAttributeName(string name, XmlNode node)
        {
            if (string.IsNullOrEmpty(name) || node == null || !node.HasChildNodes)
            {
                return null;
            }
            foreach (XmlNode p in node)
            {
                XmlAttribute attr = p.Attributes["name"];
                if (attr != null && attr.Value == name)
                {
                    return (XmlElement)p;
                }
            }
            return null;
        }

        private Type InternalGetTypeByElementAttribute(XmlNode node)
        {
            if (node == null)
            {
                return null;
            }
            XmlAttribute attr = node.Attributes["class"];
            if (attr == null)
            {
                return null;
            }
            string type = attr.Value;
            if (string.IsNullOrEmpty(type))
            {
                return null;
            }
            return Type.GetType(type, false);
        }

        private object InternalConvertToValue(string value, Type type)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            if (type == typeof(string))
            {
                return value;
            }
            MethodInfo met = type.GetMethod("TryParse", new Type[] { typeof(string), typeof(NumberStyles),
typeof(IFormatProvider), type.MakeByRefType() });
            object[] args = null;
            if (met == null)
            {
                met = type.GetMethod("TryParse", new Type[] { typeof(string), type.MakeByRefType() });
                if (met == null)
                {
                    return null;
                }
                args = new object[] { value, null };
            }
            else
            {
                args = new object[] { value, NumberStyles.Float | NumberStyles.Number, null, null };
            }
            if (args == null)
            {
                return null;
            }
            if ((true).Equals(met.Invoke(null, args)))
            {
                return args[args.Length - 1];
            }
            return null;
        }

        private object InternalGetXmlElementToValue(bool __bIsEleAttrValue, string ___sProperyNameIterName, XmlElement __objRefIterXmlElement, Type __objRefIterType)
        {
            string ___sProperyNameIterValue = null;
            if (__bIsEleAttrValue)
            {
                ___sProperyNameIterValue = __objRefIterXmlElement.GetAttribute(___sProperyNameIterName);
            }
            else
            {
                ___sProperyNameIterValue = __objRefIterXmlElement.InnerText;
            }
            return InternalConvertToValue(___sProperyNameIterValue, __objRefIterType);
        }

        private void InternalTraversalDeserialize(object __objRef, Type __objRefType, XmlNode __objRefXmlRootNode)
        {
            if (__objRef != null && __objRefType != null && __objRefXmlRootNode != null && __objRefXmlRootNode.HasChildNodes)
            {
                foreach (PropertyInfo _sProperyNameIter in __objRefType.GetProperties())
                {
                    string ___sProperyNameIterName = InternalGetXmlElementName(_sProperyNameIter);
                    if (string.IsNullOrEmpty(___sProperyNameIterName))
                    {
                        ___sProperyNameIterName = _sProperyNameIter.Name;
                    }
                    XmlElement __objRefIterXmlElement = InternalGetElementByAttributeName(___sProperyNameIterName, __objRefXmlRootNode);
                    bool __bIsEleAttrValue = false;
                    if (__objRefIterXmlElement == null)
                    {
                        ___sProperyNameIterName = InternalGetXmlAttributeName(_sProperyNameIter);
                        if (string.IsNullOrEmpty(___sProperyNameIterName))
                        {
                            if (!InternalHasXmlAttribute(_sProperyNameIter))
                            {
                                continue;
                            }
                            ___sProperyNameIterName = _sProperyNameIter.Name;
                        }
                        __bIsEleAttrValue = true;
                        __objRefIterXmlElement = (XmlElement)__objRefXmlRootNode;
                    }
                    Type __objRefIterType = _sProperyNameIter.PropertyType;
                    bool __bIsBaseType = __slstBaseTypeTable.Contains(__objRefIterType);
                    if (__bIsBaseType)
                    {
                        object __objIterPropValue = InternalGetXmlElementToValue(__bIsEleAttrValue, ___sProperyNameIterName, __objRefIterXmlElement, __objRefIterType); ;
                        if (__objIterPropValue != null)
                        {
                            _sProperyNameIter.SetValue(__objRef, __objIterPropValue, null);
                        }
                    }
                    else
                    {
                        if (__objRefIterType == typeof(object))
                        {
                            __objRefIterType = InternalGetTypeByElementAttribute(__objRefIterXmlElement);
                        }
                        if (__objRefIterType.IsArray && __objRefIterXmlElement.HasChildNodes)
                        {
                            Type __objRefIterArrType = InternalGetStandardType(__objRefIterType);
                            if (__objRefIterArrType == typeof(object))
                            {
                                __objRefIterType = InternalGetTypeByElementAttribute(__objRefIterXmlElement);
                                __objRefIterArrType = InternalGetStandardType(__objRefIterType);
                            }
                            Array __objIterPropValue = Array.CreateInstance(__objRefIterArrType, __objRefIterXmlElement.ChildNodes.Count);
                            __bIsBaseType = __slstBaseTypeTable.Contains(__objRefIterArrType);
                            for (int i = 0; i < __objRefIterXmlElement.ChildNodes.Count; i++)
                            {
                                XmlNode __objIterXmlNode = __objRefIterXmlElement.ChildNodes[i];
                                int __objOffsIterXmlNode = __slstBaseTypeNameTable.IndexOf(__objIterXmlNode.Name);
                                Type __objRefFxIterArrType = __objRefIterArrType;
                                bool __bIsRealBaseType = __bIsBaseType;
                                if (__objOffsIterXmlNode > -1)
                                {
                                    __objRefFxIterArrType = __slstBaseTypeTable[__objOffsIterXmlNode];
                                    __bIsRealBaseType = true;
                                }
                                if (__bIsRealBaseType)
                                {
                                    object __objIterArrValue = InternalConvertToValue(__objIterXmlNode.InnerText, __objRefFxIterArrType);
                                    if (__objIterPropValue != null)
                                    {
                                        __objIterPropValue.SetValue(__objIterArrValue, i);
                                    }
                                }
                                else
                                {
                                    Type __objRefKxIterArrType = __objRefIterArrType;
                                    if (__objRefFxIterArrType == typeof(object))
                                    {
                                        __objRefKxIterArrType = InternalGetTypeByElementAttribute(__objIterXmlNode);
                                    }
                                    object __objIterArrValue = Activator.CreateInstance(__objRefKxIterArrType);
                                    if (__objIterArrValue == null)
                                    {
                                        continue;
                                    }
                                    __objIterPropValue.SetValue(__objIterArrValue, i);
                                    InternalTraversalDeserialize(__objIterArrValue, __objRefKxIterArrType, __objIterXmlNode);
                                }
                            }
                            _sProperyNameIter.SetValue(__objRef, __objIterPropValue, null);
                        }
                        else
                        {
                            __bIsBaseType = __slstBaseTypeTable.Contains(__objRefIterType);
                            if (__bIsBaseType)
                            {
                                object __objIterPropValue = InternalConvertToValue(__objRefIterXmlElement.InnerXml, __objRefIterType);
                                if (__objIterPropValue != null)
                                {
                                    _sProperyNameIter.SetValue(__objRef, __objIterPropValue, null);
                                }
                            }
                            else
                            {
                                object __objIterPropValue = Activator.CreateInstance(__objRefIterType);
                                if (__objIterPropValue == null)
                                {
                                    continue;
                                }
                                _sProperyNameIter.SetValue(__objRef, __objIterPropValue, null);
                                InternalTraversalDeserialize(__objIterPropValue, __objRefIterType, __objRefIterXmlElement);
                            }
                        }
                    }
                }
            }
        }
    }
}
