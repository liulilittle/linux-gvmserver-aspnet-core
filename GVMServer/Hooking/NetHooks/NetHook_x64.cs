namespace GVMServer.Hooking.NetHooks
{
    using System;
    using System.Runtime.InteropServices;

    partial class NetHook_x64 : NetHook // 48 B8 00 00 00 00 00 00 00 00 FF E0
    {
        private IntPtr mOldMethodAddress;
        private IntPtr mNewMethodAddress;
        private byte[] mOldMethodAsmCode;
        private byte[] mNewMethodAsmCode;

        public override void Install(IntPtr oldMethodAddress, IntPtr newMethodAddress)
        {
            if (oldMethodAddress == NativeMethods.NULL || newMethodAddress == NativeMethods.NULL)
                throw new Exception("The address is invalid.");
            if (!AdjustProtectMemoryPermissions(oldMethodAddress, 12, 
                ProtectMemoryPermissions.Execute | 
                ProtectMemoryPermissions.Read |
                ProtectMemoryPermissions.Write))
                throw new Exception("Unable to modify memory protection.");
            this.mOldMethodAddress = oldMethodAddress;
            this.mNewMethodAddress = newMethodAddress;
            this.mOldMethodAsmCode = this.GetHeadCode(this.mOldMethodAddress);
            this.mNewMethodAsmCode = this.ConvetToBinary(this.mNewMethodAddress.ToInt64());
            this.mNewMethodAsmCode = this.CombineOfArray(new byte[] { 0x48, 0xB8 }, this.mNewMethodAsmCode);
            this.mNewMethodAsmCode = this.CombineOfArray(this.mNewMethodAsmCode, new byte[] { 0xFF, 0xE0 });
            if (!this.WriteToMemory(this.mNewMethodAsmCode, this.mOldMethodAddress, 12))
                throw new Exception("Cannot be written to memory.");
        }

        public override void Suspend()
        {
            if (this.mOldMethodAddress == NativeMethods.NULL)
                throw new Exception("Unable to suspend.");
            this.WriteToMemory(this.mOldMethodAsmCode, this.mOldMethodAddress, 12);
        }

        public override void Resume()
        {
            if (this.mOldMethodAddress == NativeMethods.NULL)
                throw new Exception("Unable to resume.");
            this.WriteToMemory(this.mNewMethodAsmCode, this.mOldMethodAddress, 12);
        }

        public override void Uninstall()
        {
            if (this.mOldMethodAddress == NativeMethods.NULL)
                throw new Exception("Unable to uninstall.");
            if (!this.WriteToMemory(this.mOldMethodAsmCode, this.mOldMethodAddress, 12))
                throw new Exception("Cannot be written to memory.");
            this.mOldMethodAsmCode = null;
            this.mNewMethodAsmCode = null;
            this.mOldMethodAddress = NativeMethods.NULL;
            this.mNewMethodAddress = NativeMethods.NULL;
        }

        private byte[] GetHeadCode(IntPtr ptr)
        {
            byte[] buffer = new byte[12];
            Marshal.Copy(ptr, buffer, 0, 12);
            return buffer;
        }

        private byte[] ConvetToBinary(long num)
        {
            byte[] buffer = new byte[8];
            unsafe
            {
                fixed (byte* p = buffer)
                {
                    *(long*)p = num;
                }
            }
            return buffer;
        }

        private byte[] CombineOfArray(byte[] x, byte[] y)
        {
            int i = 0, len = x.Length;
            byte[] buffer = new byte[len + y.Length];
            while (i < len)
            {
                buffer[i] = x[i];
                i++;
            }
            while (i < buffer.Length)
            {
                buffer[i] = y[i - len];
                i++;
            }
            return buffer;
        }

        private bool WriteToMemory(byte[] buffer, IntPtr address, int size)
        {
            if (size < 0 || (buffer == null && 0 != size))
            {
                return false;
            }
            if (address == IntPtr.Zero && 0 != size)
            {
                return false;
            }
            Marshal.Copy(buffer, 0, address, Convert.ToInt32(size));
            return true;
        }
    }
}
