namespace GVMServer.Hooking.NetHooks
{
    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;

    abstract class NetHook
    {
        public abstract void Install(IntPtr oldMethodAddress, IntPtr newMethodAddress);

        public abstract void Suspend();

        public abstract void Resume();

        public abstract void Uninstall();

        public IntPtr GetProcAddress(Delegate d)
        {
            if (d == null)
            {
                return IntPtr.Zero;
            }
            return Marshal.GetFunctionPointerForDelegate(d);
        }

        public IntPtr GetProcAddress(MethodBase m)
        {
            if (m == null)
            {
                return IntPtr.Zero;
            }
            return m.MethodHandle.GetFunctionPointer();
        }

        public static NetHook CreateInstance() // ::IsWow64Process
        {
            if (IntPtr.Size != sizeof(int)) // Environment.Is64BitProcess
            {
                return new NetHook_x64();
            }
            return new NetHook_x86();
        }

        public static NetHook CreateInstance(IntPtr oldMethodAddress, IntPtr newMethodAddress)
        {
            NetHook hook = NetHook.CreateInstance();
            try
            {
                return hook;
            }
            finally
            {
                hook.Install(oldMethodAddress, newMethodAddress);
            }
        }

        public static NetHook CreateInstance(IntPtr oldMethodAddress, Delegate newMethodDelegate)
        {
            return NetHook.CreateInstance(oldMethodAddress, Marshal.GetFunctionPointerForDelegate(newMethodDelegate));
        }

        protected abstract partial class NativeMethods
        {
            public static readonly IntPtr NULL = IntPtr.Zero;

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool VirtualProtect(IntPtr lpAddress, int dwSize, int flNewProtect, out int lpflOldProtect);

            [DllImport("libc", SetLastError = true)]
            public unsafe static extern int mprotect(IntPtr start, int len, int prot);
        }

        public enum ProtectMemoryPermissions
        {
            NoAccess = 0,
            Write = 1,
            Read = 2,
            Execute = 4,
        }

        private enum VirtualAllocationProtect
        {
            PAGE_EXECUTE = 0x00000010,
            PAGE_EXECUTE_READ = 0x00000020,
            PAGE_EXECUTE_READWRITE = 0x00000040,
            PAGE_EXECUTE_WRITECOPY = 0x00000080,
            PAGE_NOACCESS = 0x00000001,
            PAGE_READONLY = 0x00000002,
            PAGE_READWRITE = 0x00000004,
            PAGE_WRITECOPY = 0x00000008,
            PAGE_GUARD = 0x00000100,
            PAGE_NOCACHE = 0x00000200,
            PAGE_WRITECOMBINE = 0x00000400
        }

        private enum UnixAllocationProtect
        {
            PROT_NONE = 0x0, /* page can not be accessed */
            PROT_READ = 0x1, /* page can be read */
            PROT_WRITE = 0x2, /* page can be written */
            PROT_EXEC = 0x4, /* page can be executed */
        }

        public static bool AdjustProtectMemoryPermissions(IntPtr memory, int counts, ProtectMemoryPermissions permissions)
        {
            if (memory == IntPtr.Zero && counts != 0)
            {
                return false;
            }
            if (counts == 0)
            {
                return true;
            }
            int privileges = 0;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                if (permissions == ProtectMemoryPermissions.NoAccess)
                {
                    privileges |= (int)VirtualAllocationProtect.PAGE_NOACCESS;
                }
                else
                {
                    bool executing = 0 != (permissions & ProtectMemoryPermissions.Execute);
                    if (0 != (permissions & ProtectMemoryPermissions.Read))
                    {
                        if (executing)
                        {
                            privileges = (int)VirtualAllocationProtect.PAGE_EXECUTE_READ;
                        }
                        else
                        {
                            privileges = (int)VirtualAllocationProtect.PAGE_READONLY;
                        }
                    }
                    if (0 != (permissions & ProtectMemoryPermissions.Write))
                    {
                        if (executing)
                        {
                            privileges = (int)VirtualAllocationProtect.PAGE_EXECUTE_READWRITE;
                        }
                        else
                        {
                            privileges = (int)VirtualAllocationProtect.PAGE_READWRITE;
                        }
                    }
                }
                return NativeMethods.VirtualProtect(memory, counts, privileges, out int flOldProtect);
            }
            else
            {
                if (permissions == ProtectMemoryPermissions.NoAccess)
                {
                    privileges = (int)UnixAllocationProtect.PROT_NONE;
                }
                else
                {
                    if (0 != (permissions & ProtectMemoryPermissions.Read))
                    {
                        privileges |= (int)UnixAllocationProtect.PROT_READ;
                    }
                    if (0 != (permissions & ProtectMemoryPermissions.Write))
                    {
                        privileges |= (int)UnixAllocationProtect.PROT_WRITE;
                    }
                    if (0 != (permissions & ProtectMemoryPermissions.Write))
                    {
                        privileges |= (int)UnixAllocationProtect.PROT_EXEC;
                    }
                }
                return NativeMethods.mprotect(memory, counts, privileges) >= 0;
            }
        }
    }
}
