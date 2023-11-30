namespace GVMServer.AOP.Mock
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;

    static class MockEmitModule
    {
        private static readonly ModuleBuilder mModule;
        private static readonly AssemblyBuilder mAssembly;

        static MockEmitModule()
        {
            AssemblyName name = new AssemblyName("Mock4NetR Anonymous Dynamic Module");
#if NETCOREAPP2_0
            mAssembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
#else
            mAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndSave);
#endif
            mModule = mAssembly.DefineDynamicModule(name.Name);
        }

        public static ModuleBuilder Module
        {
            get
            {
                return mModule;
            }
        }

        public static int ModuleToken(Module m)
        {
            return m.GetHashCode();
        }

        public static Module ResolveModule(int moduleToken)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Module m in assembly.GetModules())
                {
                    if (m.GetHashCode() == moduleToken)
                        return m;
                }
            }
            return null;
        }
    }
}
