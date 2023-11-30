namespace GVMServer.AOP.Mock
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    static class MockTypeEngine
    {
        private const FieldAttributes FIELD_ATTRIBUTES = FieldAttributes.Private;
        private const TypeAttributes TYPE_ATTRIBUTES = TypeAttributes.Public | TypeAttributes.Sealed |
            TypeAttributes.Serializable;
        private const CallingConventions CALLING_CONVENTIONS = CallingConventions.HasThis;
        private const PropertyAttributes PROPERTY_ATTRIBUTES = PropertyAttributes.SpecialName;

        public static Type Create(Type src)
        {
            bool abstractc = !src.IsInterface && src.IsAbstract && !src.IsSealed;
            TypeBuilder root = MockEmitModule.Module.CreateType("Foo", abstractc ? src : null, abstractc ? null : new[] { src });
            FieldBuilder handler = root.CreateHandler();
            root.CreateMembers(src, handler);
            root.CreateConstructor(handler);
            return root.CreateType();
        }

        private static TypeBuilder CreateType(this ModuleBuilder builder, string name, Type parent, Type[] interfaces)
        {
            return builder.DefineType(name, TYPE_ATTRIBUTES, parent, interfaces);
        }

        private static void CreateMembers(this TypeBuilder builder, Type src, FieldBuilder handler)
        {
            IDictionary<string, MethodBuilder> map = new Dictionary<string, MethodBuilder>();
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
            foreach (MethodInfo m in src.GetMethods(flags))
            {
                if (m.IsAbstract)
                {
                    map.Add(m.Name, builder.CreateMethod(m, handler));
                }
            }
            foreach (PropertyInfo p in src.GetProperties(flags))
            {
                builder.CreateProperty(p, map);
            }
        }

        private static void CreateProperty(this TypeBuilder builder, PropertyInfo p, IDictionary<string, MethodBuilder> map)
        {
            Action<MethodInfo, Action<MethodBuilder>> safebind = (method, bind) =>
            {
                MethodBuilder kv;
                if (!map.TryGetValue(method.Name, out kv))
                {
                    bind(kv);
                }
            };
            MethodInfo get = p.GetGetMethod();
            MethodInfo set = p.GetSetMethod();
            PropertyBuilder pb = builder.DefineProperty(p.Name, p.Attributes, p.PropertyType, null);
            if (get != null)
            {
                safebind(get, pb.SetGetMethod);
            }
            if (set != null)
            {
                safebind(set, pb.SetSetMethod);
            }
        }

        private static void CreateConstructor(this TypeBuilder builder, FieldBuilder handler)
        {
            Type[] args = new Type[] { typeof(IInvocationHandler) };
            ConstructorBuilder ctor = builder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, args);
            ILGenerator il = ctor.GetILGenerator();
            //
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, handler);
            il.Emit(OpCodes.Ret);
        }

        private static FieldBuilder CreateHandler(this TypeBuilder builder)
        {
            return builder.DefineField("m_handler", typeof(IInvocationHandler), FIELD_ATTRIBUTES);
        }
    }
}
