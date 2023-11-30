namespace GVMServer.AOP.Mock
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;

    sealed class MockMethodILWeaver
    {
        public FieldInfo ProxyHandler;
        public int MethodToken;
        public int ModuleToken;
        public Type ReturnType;
        public string MethodName;
        public Type[] ParameterType;

        public void CompileTo(ILGenerator IL)
        {
            IL.DeclareLocal(typeof(object[]));
            if (ReturnType != typeof(void))
                IL.DeclareLocal(ReturnType);
            IL.Emit(OpCodes.Ldc_I4, ParameterType.Length);
            IL.Emit(OpCodes.Newarr, typeof(object));
            IL.Emit(OpCodes.Stloc_0);
            for (int i = 0; i < ParameterType.Length; i++)
            {
                IL.Emit(OpCodes.Ldloc_0);
                IL.Emit(OpCodes.Ldc_I4, i);
                IL.Emit(OpCodes.Ldarg, i + 1);
                Type refer = ParameterType[i];
                if (refer.IsByRef || refer.IsPointer)
                    IL.op_ldind(refer);
                else
                    IL.Emit(OpCodes.Box, refer);
                IL.Emit(OpCodes.Stelem_Ref);
            }
            IL.Emit(OpCodes.Ldarg_0);
            IL.Emit(OpCodes.Ldfld, ProxyHandler);
            IL.Emit(OpCodes.Ldarg_0);
            IL.Emit(OpCodes.Ldc_I4, MethodToken);
            IL.Emit(OpCodes.Ldstr, MethodName);
            IL.Emit(OpCodes.Ldc_I4, ModuleToken);
            IL.Emit(OpCodes.Ldloc_0);
            IL.Emit(OpCodes.Call, typeof(IInvocationHandler).GetMethod("InvokeMember", BindingFlags.Instance | BindingFlags.Public));
            if (ReturnType == typeof(void))
                IL.Emit(OpCodes.Pop);
            else
            {
                IL.op_unbox_any(ReturnType);
                IL.Emit(OpCodes.Stloc_1);
                IL.Emit(OpCodes.Ldloc_1);
            }
            for (int i = 0; i < ParameterType.Length; i++)
            {
                Type refer = ParameterType[i];
                if (refer.IsByRef || refer.IsPointer)
                {
                    IL.Emit(OpCodes.Ldarg, i + 1);
                    IL.Emit(OpCodes.Ldloc_0);
                    IL.Emit(OpCodes.Ldc_I4, i);
                    IL.Emit(OpCodes.Ldelem_Ref);
                    IL.op_stind(refer);
                }
            }
            IL.Emit(OpCodes.Ret);
        }
    }
}
