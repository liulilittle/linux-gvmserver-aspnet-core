namespace GVMServer.Planning.PlanningXml
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Xml;
    using GVMServer.IO;

    class Debugger
    {
        public static void LogErr(string str)
        {
            Console.WriteLine(str);
        }
    }

    class XmlHandler
    {
        private XmlDocument createXmlDoc(string filepath)
        {
            XmlDocument doc;
            try
            {
                doc = new XmlDocument();
                byte[] buffer = File.ReadAllBytes(filepath);
                Encoding ec = FileAuxiliary.GetEncoding(buffer);
                string content = ec.GetString(buffer);

                doc.LoadXml(content);
            }
            catch (Exception e)
            {
                throw new Exception(e.ToString());
            }

            return doc;
        }

        private void TryParseDataType(string ty, ref StructRow row)
        {
            if (ty.Equals("uint8"))
            {
                row.Type_ = typeof(byte);
            }
            else if (ty.Equals("uint16"))
            {
                row.Type_ = typeof(UInt16);
            }
            else if (ty.Equals("uint32"))
            {
                row.Type_ = typeof(UInt32);
            }
            else if (ty.Equals("uint64"))
            {
                row.Type_ = typeof(UInt64);
            }
            else if (ty.Equals("int8"))
            {
                row.Type_ = typeof(byte);
            }
            else if (ty.Equals("int16"))
            {
                row.Type_ = typeof(Int16);
            }
            else if (ty.Equals("int32"))
            {
                row.Type_ = typeof(Int32);
            }
            else if (ty.Equals("int64"))
            {
                row.Type_ = typeof(Int64);
            }
            else if (ty.Equals("string"))
            {
                row.Type_ = typeof(string);
            }
            else
            {
                TryParseComplexType(ty, ref row);
            }
        }

        // 此处需要照顾到上下文，遇到type = struct的形式，需要先放置标记在此处
        private void TryParseComplexType(string ty, ref StructRow row)
        {
            ComplexDataType t;
            if (DataModuleMgr.Instance.ComplexDataTypes.TryGetValue(ty, out t))
            {
                row.ComplexDataTypeValue = t;
                row.Type_ = typeof(ComplexDataType);
                row.ComplexTypeDeclareName = String.Empty;
            }
            else
            {
                if (row.ComplexDataTypeValue != null)
                {
                    ComplexDataType.Release(row.ComplexDataTypeValue);
                    row.ComplexDataTypeValue = null;
                }
                row.ComplexTypeDeclareName = ty;
                row.Type_ = typeof(ComplexDataType);
            }
        }

        public void InitComplexDataStructs(List<string> filepaths)
        {
            foreach(string filepath in(filepaths))
            {
                XmlDocument doc = this.createXmlDoc(filepath);
                if (null != doc)
                {
                    XmlNode metalib = doc.SelectSingleNode("metalib");
                    if (metalib == null)
                        continue;
                    #region init_macros
                    do
                    {
                        // init macros
                        XmlNodeList macros = metalib.SelectNodes("macro");
                        if (macros == null || macros.Count == 0)
                            break;

                        foreach (XmlNode macro in (macros))
                        {
                            XmlAttribute macroName = macro.Attributes["name"];
                            if (macroName == null)
                                continue;

                            XmlAttribute value = macro.Attributes["value"];
                            if (value == null)
                                continue;

                            uint macroValue = 0;
                            uint.TryParse(value.Value, out macroValue);

                            DataModuleMgr.Instance._macros[macroName.Value] = macroValue;
                        }
                    } while (false);
                    #endregion

                    #region init_macrogroups
                    do
                    {
                        // init macrosgroups
                        XmlNodeList macrogroups = metalib.SelectNodes("macrosgroup");
                        if (macrogroups == null)
                            break;

                        StringBuilder sb = new StringBuilder();
                        foreach (XmlNode macrogroup in (macrogroups))
                        {
                            XmlAttribute macrogroupName = macrogroup.Attributes["name"];
                            if (macrogroupName == null)
                                continue;
                            XmlNodeList macros = macrogroup.SelectNodes("macro");
                            foreach(XmlNode macro in (macros))
                            {
                                XmlAttribute name = macro.Attributes["name"];
                                if (name == null)
                                    continue;

                                XmlAttribute value = macro.Attributes["value"];
                                if (value == null)
                                    continue;

                                uint macroValue = 0;
                                uint.TryParse(value.Value, out macroValue);
                                sb.Clear();

                                string macroKey = sb.Append(macrogroupName.Value).Append(".").Append(name.Value).ToString();
                                if (!DataModuleMgr.Instance._macros.ContainsKey(macroKey))
                                    DataModuleMgr.Instance._macros.Add(macroKey, macroValue);
                            }
                        }
                    } while (false);
                    #endregion

                    #region init_structs
                    do
                    {
                        XmlNodeList structs = metalib.SelectNodes("struct");
                        foreach (XmlNode struct_ in (structs))
                        {
                            XmlNode structKey = struct_.Attributes["name"];
                            if (null == structKey)
                                continue;
                            if (DataModuleMgr.Instance.ComplexDataTypes.ContainsKey(structKey.Value))
                            {
                                Debugger.LogErr("struct type multi declare error, key is " + structKey.Value);
                                continue;
                            }
                                
                            ComplexDataType cdt = ComplexDataType.Get();

                            XmlNodeList entrys = struct_.SelectNodes("entry");
                            foreach(XmlNode entry in (entrys))
                            {
                                do
                                {
                                    XmlAttribute name = entry.Attributes["name"];
                                    if (null == name)
                                        break;

                                    XmlAttribute t = entry.Attributes["type"];
                                    if (null == name)
                                        break;

                                    XmlAttribute c = entry.Attributes["count"];

                                    StructRow row = StructRow.Get();
                                    
                                    row.Name = name.Value;
                                    // 此处如果发现还没定义struct，先保留一个string占位，类似与c++前向引用声明
                                    TryParseDataType(t.Value, ref row);

                                    uint arrCount;
                                    if (c != null && DataModuleMgr.Instance._macros.TryGetValue(c.Value, out arrCount))
                                    {
                                        row.ArrayCount = arrCount;
                                    } 

                                    cdt.Rows.Add(row);
                                } while (false);
                            }
                            
                            DataModuleMgr.Instance.ComplexDataTypes.Add(structKey.Value, cdt);
                        }
                    } while (false);
                    #endregion
                }
            }

        }
        
        public DataModule CreateModule(string filepath)
        {
            XmlDocument doc;
            doc = this.createXmlDoc(filepath);
            if (doc == null)
                return null;

            int find = filepath.LastIndexOf("\\");
            if (find == -1)
            {
                Debugger.LogErr("not find config title: " + filepath);
                return null;
            }
            string name = filepath.Substring(find + 1);
            find = name.LastIndexOf(".");
            if (find == -1)
            {
                Debugger.LogErr("invalid config postfix: " + filepath);
                return null;
            }
            name = name.Substring(0, find);

            DataModule m = DataModule.Get();
            XmlNode xnAllCfg = doc.SelectSingleNode(name + "_Tab");
            if (xnAllCfg != null)
            {
                XmlNodeList rows = xnAllCfg.ChildNodes;
                foreach (XmlNode row in (rows))
                {
                    HandleRow(row, ref m);
                }
            }

            return m;
        }

        private void HandleRow(XmlNode node, ref DataModule module)
        {
            if (node.Name == "TResHeadAll")
                return;

            string structType = node.Name;
            ComplexDataType cdt;
            DataModuleMgr.Instance.ComplexDataTypes.TryGetValue(structType, out cdt);
            if (null == cdt)
                return;

            Dictionary<string, object> container = new Dictionary<string, object>();
            HandleData(node, ref container, cdt);
            module.PushRowData(container);
        }

        private void HandleData(XmlNode node, ref Dictionary<string, object> container, ComplexDataType cdt)
        {
            for(int i = 0; i < cdt.Rows.Count; ++i)
            {
                StructRow row = cdt.Rows[i];
                string key = row.Name;
                object val = null;
                // 数组
                if (row.ArrayCount != 0 && row.ArrayCount != 1)
                {
                    List<object> list = new List<object>();

                    XmlNodeList targetChds = node.SelectNodes(key);
                    // 基本类型数组
                    if (row.Type_ != null && row.Type_ != typeof(ComplexDataType))
                    {
                        HandleXmlValue handler = SerializationHandlers.GetValueParser(row.Type_);

                        XmlNode targetNode = node.SelectSingleNode(key);
                        if (targetNode != null)
                        {
                            string value = targetNode.InnerText;
                            string[] valueArr = value.Split(' ');
                            for (int j = 0; j < valueArr.Length; ++j)
                            {
                                if (!string.IsNullOrEmpty(valueArr[j]))
                                    list.Add(handler(valueArr[j]));
                            }
                        }
                        else
                        {
                            Debugger.LogErr("invalid basetype: " + key);
                        }
                    }
                    else
                    {
                        if (row.ComplexDataTypeValue == null && row.ComplexTypeDeclareName != null && !string.IsNullOrEmpty(row.ComplexTypeDeclareName))
                        {
                            ComplexDataType find;
                            if (DataModuleMgr.Instance.ComplexDataTypes.TryGetValue(row.ComplexTypeDeclareName, out find))
                            {
                                if (row.ComplexDataTypeValue != null)
                                {
                                    ComplexDataType.Release(row.ComplexDataTypeValue);
                                    row.ComplexDataTypeValue = find;
                                }
                                
                                row.Type_ = typeof(ComplexDataType);
                                row.ComplexTypeDeclareName = string.Empty;
                            }
                            else
                            {
                                Debugger.LogErr("untyped struct row: " + row.ComplexTypeDeclareName);
                            }
                        }

                        if (row.ComplexDataTypeValue != null)
                        {
                            XmlNodeList chList = node.SelectNodes(key);
                            foreach (XmlNode chNode in chList)
                            {
                                Dictionary<string, object> chContainer = new Dictionary<string, object>();
                                HandleData(chNode, ref chContainer, row.ComplexDataTypeValue);
                                list.Add(chContainer);
                            }
                        }
                    }
                    
                    val = list as object;
                }
                else
                {
                    if (row.Type_ != null && row.Type_ != typeof(ComplexDataType))
                    {
                        HandleXmlValue handler = SerializationHandlers.GetValueParser(row.Type_);

                        XmlNode targetNode = node.SelectSingleNode(key);
                        if (targetNode != null)
                        {
                            string value = targetNode.InnerText;
                            val = handler(value);
                        }
                        else
                        {
                            Debugger.LogErr("invalid basetype: " + key);
                        }
                    }
                    else
                    {
                        if (row.ComplexDataTypeValue == null && row.ComplexTypeDeclareName != null && !string.IsNullOrEmpty(row.ComplexTypeDeclareName))
                        {
                            ComplexDataType find;
                            if (DataModuleMgr.Instance.ComplexDataTypes.TryGetValue(row.ComplexTypeDeclareName, out find))
                            {
                                row.ComplexDataTypeValue = find;
                                row.Type_ = typeof(ComplexDataType);
                                row.ComplexTypeDeclareName = string.Empty;
                            }
                            else
                            {
                                Debugger.LogErr("untyped struct row: " + row.ComplexTypeDeclareName);
                            }
                        }

                        if (row.ComplexDataTypeValue != null)
                        {
                            Dictionary<string, object> chContainer = new Dictionary<string, object>();
                            XmlNode chNode = node.SelectSingleNode(key);
                            HandleData(chNode, ref chContainer, row.ComplexDataTypeValue);

                            val = chContainer;
                        }
                    }                    
                }

                container.Add(key, val);
            }

        }
    }
}
