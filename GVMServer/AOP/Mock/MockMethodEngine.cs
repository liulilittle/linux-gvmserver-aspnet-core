namespace GVMServer.AOP.Mock
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;

    static class MockMethodEngine
    {
        private const MethodAttributes METHOD_ATTRIBUTES = MethodAttributes.Public | MethodAttributes.Family | MethodAttributes.Virtual |  MethodAttributes.HideBySig;

        private static Type[] S(ParameterInfo[] s)
        {
            Type[] ss = new Type[s.Length];
            for (int i = 0; i < s.Length; i++)
                ss[i] = s[i].ParameterType;
            return ss;
        }

        public static MethodBuilder CreateMethod(this TypeBuilder builder, MethodInfo src, FieldInfo handler)
        {
            Type[] args = S(src.GetParameters());
            MethodBuilder mb = builder.DefineMethod(src.Name, METHOD_ATTRIBUTES, src.ReturnType, args);
            mb.SetImplementationFlags(MethodImplAttributes.IL | MethodImplAttributes.NoInlining | MethodImplAttributes.Managed);
            ILGenerator il = mb.GetILGenerator();

            MockMethodILWeaver weaver = new MockMethodILWeaver();
            weaver.MethodName = src.Name;
            weaver.MethodToken = src.MetadataToken;
            weaver.ModuleToken = MockEmitModule.ModuleToken(src.Module);
            weaver.ParameterType = args;
            weaver.ProxyHandler = handler;
            weaver.ReturnType = src.ReturnType;

            weaver.CompileTo(il);

            return mb;
        }
    }
}
