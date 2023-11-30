namespace GVMServer.AOP.Data
{
    using GVMServer.Valuetype;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Reflection.Emit;

    /// <summary>
    /// 数据模型代理转换器
    /// </summary>
    public sealed partial class DataModelProxyConverter : IDisposable
    {
        private AssemblyBuilder _assemblyBuilder = null;
        private ModuleBuilder _moduleBuilder = null;
        private volatile bool _disposed = false;
        private IDictionary<DataColumnCollection, Type> _dynamicTypeDictionary = null;

        private DataModelProxyConverter()
        {
            AssemblyName assemblyName = new AssemblyName("Platform.Aop.Dynamic");
#if NETCOREAPP2_0
            _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
#else
            _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
#endif
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule(assemblyName.Name);
            _dynamicTypeDictionary = new Dictionary<DataColumnCollection, Type>();
        }

        ~DataModelProxyConverter()
        {
            Dispose();
        }

        public void Dispose()
        {
            lock (this)
            {
                if (!_disposed)
                {
                    _disposed = true;

                    _assemblyBuilder = null;
                    _moduleBuilder = null;
                    if (_dynamicTypeDictionary != null)
                    {
                        _dynamicTypeDictionary.Clear();
                        _dynamicTypeDictionary = null;
                    }
                }
            }

            GC.SuppressFinalize(this);
        }

        private Type GetDynamicType(DataColumnCollection columns)
        {
            if (columns == null || columns.Count <= 0)
            {
                return null;
            }
            foreach (KeyValuePair<DataColumnCollection, Type> pair in _dynamicTypeDictionary)
            {
                DataColumnCollection key = pair.Key;
                if (key.Count == columns.Count)
                {
                    int equals = 0;
                    for (int i = 0; i < key.Count; i++)
                    {
                        string x = key[i].ColumnName.ToLower(), y = columns[i].ColumnName.ToLower();
                        if (key[i].DataType == columns[i].DataType && x == y)
                        {
                            equals++;
                        }
                    }
                    if (equals == key.Count)
                    {
                        return pair.Value;
                    }
                }
            }
            return null;
        }

        private Type CreateDynamicType(DataColumnCollection columns)
        {
            if (columns == null || columns.Count <= 0)
            {
                return null;
            }
            string strNewSha1TypeName = Path.GetRandomFileName();
            TypeBuilder typeBuilder = _moduleBuilder.DefineType(strNewSha1TypeName, TypeAttributes.Public);
            foreach (DataColumn column in columns)
            {
                Type colDataType = column.DataType;
                if (column.AllowDBNull && colDataType.IsValueType)
                {
                    colDataType = typeof(Nullable<>).MakeGenericType(colDataType);
                }

                FieldBuilder ldFieldBuilder = typeBuilder.DefineField("m_" + column.ColumnName, colDataType, FieldAttributes.Private);

                MethodBuilder getMethodBuilder = typeBuilder.DefineMethod(string.Format("get_{0}", colDataType),
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, colDataType, Type.EmptyTypes);
                ILGenerator il = getMethodBuilder.GetILGenerator();
                //
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, ldFieldBuilder);
                il.Emit(OpCodes.Ret);
                //
                MethodBuilder setMethodBuilder = typeBuilder.DefineMethod(string.Format("set_{0}", column.ColumnName),
                      MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(void), new Type[] { colDataType });
                il = setMethodBuilder.GetILGenerator();
                //
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, ldFieldBuilder);
                il.Emit(OpCodes.Ret);
                //
                PropertyBuilder sPropertyBuilder = typeBuilder.DefineProperty(column.ColumnName, PropertyAttributes.SpecialName, colDataType, Type.EmptyTypes);
                sPropertyBuilder.SetSetMethod(setMethodBuilder);
                sPropertyBuilder.SetGetMethod(getMethodBuilder);
            }
            return typeBuilder.CreateType();
        }

        public Type GetType(DataTable table)
        {
            if (table == null)
            {
                return null;
            }
            DataColumnCollection columns = table.Columns;
            if (columns.Count <= 0)
            {
                return null;
            }
            Type dynamicType = GetDynamicType(columns);
            if (dynamicType == null)
            {
                dynamicType = CreateDynamicType(columns);
                if (dynamicType != null)
                {
                    _dynamicTypeDictionary.Add(columns, dynamicType);
                }
            }
            return dynamicType;
        }

        public object GetObject(Type type)
        {
            return Activator.CreateInstance(type);
        }

        public IList<object> ToList( DataTable value, Type type = null )
        {
            return IntenralToList<object>( value, type );
        }

        public IList<T> ToList<T>(DataTable value)
        {
            return IntenralToList<T>( value, typeof( T ) );
        }

        private IList<T> IntenralToList<T>(DataTable value, Type type = null)
        {
            if (value == null)
            {
                throw new ArgumentNullException();
            }
            if (type == null)
            {
                type = GetType(value);
                if (type == null)
                {
                    throw new ArgumentNullException();
                }
            }
            ConstructorInfo ctor = type.GetConstructor(Type.EmptyTypes);
            if (ctor == null)
            {
                throw new ArgumentNullException();
            }
            IList<KeyValuePair<PropertyInfo, int>> properties = new List<KeyValuePair<PropertyInfo, int>>();
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                foreach (DataColumn cols in value.Columns)
                {
                    if ((cols.ColumnName).ToUpper() == (property.Name).ToUpper())
                    {
                        properties.Add(new KeyValuePair<PropertyInfo, int>(property, cols.Ordinal));
                    }
                }
            }
            IList<T> buffer = new List<T>();
            foreach (DataRow row in value.Rows)
            {
                object model = ctor.Invoke(Type.EmptyTypes);
                try
                {
                    buffer.Add((T)model);
                }
                finally
                {
                    foreach (KeyValuePair<PropertyInfo, int> pair in properties)
                    {
                        object args = row[pair.Value];
                        if (args == DBNull.Value)
                        {
                            args = null;
                        }
                        Type prop = pair.Key.PropertyType;
                        if (args != null && !prop.IsInstanceOfType(args))
                        {
                            if (prop.IsEnum)
                            {
                                prop = prop.GetEnumUnderlyingType();
                            }
                            if (Valuetype.IsFloatType(prop))
                            {
                                args = ValuetypeFormatter.Parse(Convert.ToString(args), prop, NumberStyles.Number | NumberStyles.Float);
                            }
                            else
                            {
                                args = ValuetypeFormatter.Parse(Convert.ToString(args), prop);
                            }
                        }
                        else if (args != null && prop == typeof(bool))
                        {
                            if (args is bool)
                                args = (bool)args;
                            else
                                args = (Convert.ToInt64(prop) != 0);
                        }
                        pair.Key.SetValue(model, args, null);
                    }
                }
            }
            return buffer;
        }

        private static DataModelProxyConverter _aopDynamicProxy = null;

        public static DataModelProxyConverter GetInstance()
        {
            if (_aopDynamicProxy == null)
            {
                _aopDynamicProxy = new DataModelProxyConverter();
            }
            return _aopDynamicProxy;
        }
    }
}
