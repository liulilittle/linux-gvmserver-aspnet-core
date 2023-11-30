namespace GVMServer.AOP.Mock
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;

    static class MockEmitInstruction
    {
        public static void op_ldind(this ILGenerator il, Type refer)
        {
            Type num = refer.GetElementType();
            bool box = true;
            if (num == typeof(int))
                il.Emit(OpCodes.Ldind_I4);
            else if (num == typeof(uint))
                il.Emit(OpCodes.Ldind_U4);
            else if (num == typeof(long) || num == typeof(ulong))
                il.Emit(OpCodes.Ldind_I8);
            else if (num == typeof(double))
                il.Emit(OpCodes.Ldind_R8);
            else if (num == typeof(float))
                il.Emit(OpCodes.Ldind_R4);
            else if (num == typeof(short))
                il.Emit(OpCodes.Ldind_I2);
            else if (num == typeof(ushort))
                il.Emit(OpCodes.Ldind_U2);
            else if (num == typeof(sbyte))
                il.Emit(OpCodes.Ldind_I1);
            else if (num == typeof(byte))
                il.Emit(OpCodes.Ldind_U1);
            else if (num.IsValueType)
                il.Emit(OpCodes.Ldobj, num);
            else
            {
                box = false;
                il.Emit(OpCodes.Ldind_Ref);
            }
            if (box) il.Emit(OpCodes.Box, num);
        }

        public static void op_unbox_any(this ILGenerator il, Type num)
        {
            if (num == typeof(int) || num == typeof(uint) || num == typeof(long)
                || num == typeof(ulong) || num == typeof(double) || num == typeof(float)
                || num == typeof(short) || num == typeof(ushort) || num == typeof(char)
                || num == typeof(byte) || num == typeof(sbyte) || num == typeof(bool))
            {
                MethodInfo m = typeof(Convert).GetMethod(string.Format("To{0}", num.Name), new[] { typeof(object) });
                il.Emit(OpCodes.Call, m);
            }
            else
            {
                il.Emit(OpCodes.Unbox_Any, num);
            }
        }

        public static void op_stind(this ILGenerator il, Type refer)
        {
            Type num = refer.GetElementType();
            if (num == typeof(int) || num == typeof(uint))
            {
                il.op_unbox_any(num);
                il.Emit(OpCodes.Stind_I4);
            }
            else if (num == typeof(long) || num == typeof(ulong))
            {
                il.op_unbox_any(num);
                il.Emit(OpCodes.Stind_I8);
            }
            else if (num == typeof(double))
            {
                il.op_unbox_any(num);
                il.Emit(OpCodes.Stind_R8);
            }
            else if (num == typeof(float))
            {
                il.op_unbox_any(num);
                il.Emit(OpCodes.Stind_R4);
            }
            else if (num == typeof(short) || num == typeof(ushort) || num == typeof(char))
            {
                il.op_unbox_any(num);
                il.Emit(OpCodes.Stind_I2);
            }
            else if (num == typeof(byte) || num == typeof(sbyte) || num == typeof(bool))
            {
                il.op_unbox_any(num);
                il.Emit(OpCodes.Stind_I1);
            }
            else if (num.IsValueType)
            {
                il.op_unbox_any(num);
                il.Emit(OpCodes.Stobj, num);
            }
            else
            {
                il.Emit(OpCodes.Stind_Ref);
            }
        }
    }
}
