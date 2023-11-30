namespace GVMServer.Planning.PlanningXml
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    class StructRow : PoolGetRelease<StructRow>, IPoolObject
    {
        public string Name;
        public Type Type_;
        public string ComplexTypeDeclareName;
        public ComplexDataType ComplexDataTypeValue;
        // ignore cname
        // ignore desc
        public uint ArrayCount;

        public void Init()
        {
            Name = String.Empty;
            Type_ = null;
            ComplexTypeDeclareName = String.Empty;
            ComplexDataTypeValue = ComplexDataType.Get();
            ArrayCount = 1;
        }

        public void Reset()
        {
            Name = String.Empty;
            Type_ = null;
            ComplexTypeDeclareName = String.Empty;
            if (ComplexDataTypeValue != null)
            {
                ComplexDataType.Release(ComplexDataTypeValue);
                ComplexDataTypeValue = null;
            }
                
            ArrayCount = 1;
        }
    }

    class ComplexDataType : PoolGetRelease<ComplexDataType>, IPoolObject
    {
        public List<StructRow> Rows { get; set; }

        public string Name { get; set; }

        public void Init()
        {
            Rows = new List<StructRow>();
            Name = string.Empty;
        }

        public void Reset()
        {
            foreach (StructRow row in (Rows))
            {
                StructRow.Release(row);
            }
            Rows.Clear();
            Name = string.Empty;
        }
    }

    class DataModule: PoolGetRelease<DataModule>, IPoolObject, IConfigurationLoader
    {
        private List<object> _data;

        public ComplexDataType Row { set; get; }

        public void Init()
        {
            _data = new List<object>();
            Row = null;
        }

        public void Reset()
        {
            _data.Clear();
            Row = null;
        }

        public void PushRowData(params object[] rows)
        {
            _data.AddRange(rows);
        }

        public IList<T> GetAll<T>() where T : class
        {
            List<T> retList = new List<T>();
            foreach(object row in _data)
            {
                retList.Add(SetModelValue(typeof(T), row as Dictionary<string, object>) as T);
            }
            
            return retList;
        }

        private object SetModelValue(Type T_, Dictionary<string, object> map)
        {
            var model = T_.GetConstructor(new Type[] { }).Invoke(new object[] { });
            PropertyInfo[] properties = T_.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            Type t = model.GetType();

            foreach (PropertyInfo pty in properties)
            {
                object value;
                if (!map.TryGetValue(pty.Name, out value))
                    continue;

                if (!DataModule.IsArrayType(pty.PropertyType))
                {
                    if (DataModule.IsDataType(pty.PropertyType))
                        pty.SetValue(model, value, null);
                    else
                    {
                        pty.SetValue(model, SetModelValue(pty.PropertyType, value as Dictionary<string, object>), null);
                    }
                }
                else
                {
                    List<object> arr = value as List<object>;
                    
                    if (pty.PropertyType.IsArray)
                    {
                        Type eleType = pty.PropertyType.GetElementType();
                        if (pty.GetValue(model) == null)
                        {
                            Array arrInst = Array.CreateInstance(eleType, arr.Count);
                            for (int i = 0; i < arr.Count; ++i)
                            {
                                object eachValue = arr[i];
                                if (DataModule.IsDataType(eleType))
                                {
                                    arrInst.SetValue(eachValue, i);
                                }
                                else
                                {
                                    arrInst.SetValue(SetModelValue(eleType, eachValue as Dictionary<string, object>), i);
                                }
                            }
                            pty.SetValue(model, arrInst);
                        }
                    }
                    else
                    {
                        Type elementType = pty.PropertyType.GetMethod("Find").ReturnType;
                        Type listType = typeof(List<>).MakeGenericType(pty.PropertyType);
                        object list = Activator.CreateInstance(listType);
                        MethodInfo addMethod = listType.GetMethod("Add");
                        foreach (object eachValue in arr)
                        {
                            if (DataModule.IsDataType(elementType))
                            {
                                addMethod.Invoke((object)list, new object[] { eachValue });
                            }
                            else
                            {
                                addMethod.Invoke((object)list, new object[] { SetModelValue(elementType, eachValue as Dictionary<string, object>) });
                            }
                        }
                    } 
                }
            }
            return model;
        }

        public static bool IsArrayType(Type t)
        {
            return t.IsArray
                || (t.IsGenericType && (typeof(IList<>).GUID == t.GUID || typeof(List<>).GUID == t.GUID));
        }

        public static bool IsDataType(Type t)
        {
            return t == typeof(byte)
                || t == typeof(UInt16)
                || t == typeof(UInt32)
                || t == typeof(UInt64)
                || t == typeof(int)
                || t == typeof(uint)
                || t == typeof(Int16)
                || t == typeof(Int32)
                || t == typeof(Int64)
                || t == typeof(string);
        }
    }
}
