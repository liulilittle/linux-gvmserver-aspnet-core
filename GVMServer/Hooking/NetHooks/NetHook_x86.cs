namespace GVMServer.Hooking.NetHooks
{
    using System;
    using System.Runtime.InteropServices;

    partial class NetHook_x86 : NetHook // E9 00 00 00 00
    {
        private IntPtr mOldMethodAddress;
        private IntPtr mNewMethodAddress;
        private byte[] mOldMethodAsmCode;
        private byte[] mNewMethodAsmCode;

        public override void Install(IntPtr oldMethodAddress, IntPtr newMethodAddress)
        {
            if (oldMethodAddress == NativeMethods.NULL || newMethodAddress == NativeMethods.NULL)
                throw new Exception("The address is invalid.");
            if (!AdjustProtectMemoryPermissions(oldMethodAddress))
                throw new Exception("Unable to modify memory protection.");
            this.mOldMethodAddress = oldMethodAddress;
            this.mNewMethodAddress = newMethodAddress;
            this.mOldMethodAsmCode = this.GetHeadCode(this.mOldMethodAddress);
            this.mNewMethodAsmCode = this.ConvetToBinary(Convert.ToInt32(this.mNewMethodAddress.ToInt64() - (this.mOldMethodAddress.ToInt64() + 5)));
            this.mNewMethodAsmCode = this.CombineOfArray(new byte[] { 0xE9 }, this.mNewMethodAsmCode);
            if (!this.WriteToMemory(this.mNewMethodAsmCode, this.mOldMethodAddress, 5))
                throw new Exception("Cannot be written to memory.");
        }

        protected virtual bool AdjustProtectMemoryPermissions(IntPtr address)
        {
            return AdjustProtectMemoryPermissions(address, 5,
                      ProtectMemoryPermissions.Execute |
                      ProtectMemoryPermissions.Read |
                      ProtectMemoryPermissions.Write);
        }

        public override void Suspend()
        {
            if (this.mOldMethodAddress == NativeMethods.NULL)
                throw new Exception("Unable to suspend.");
            this.WriteToMemory(this.mOldMethodAsmCode, this.mOldMethodAddress, 5);
        }

        public override void Resume()
        {
            if (this.mOldMethodAddress == NativeMethods.NULL)
                throw new Exception("Unable to resume.");
            this.WriteToMemory(this.mNewMethodAsmCode, this.mOldMethodAddress, 5);
        }

        public override void Uninstall()
        {
            if (this.mOldMethodAddress == NativeMethods.NULL)
                throw new Exception("Unable to uninstall.");
            if (!this.WriteToMemory(this.mOldMethodAsmCode, this.mOldMethodAddress, 5))
                throw new Exception("Cannot be written to memory.");
            this.mOldMethodAsmCode = null;
            this.mNewMethodAsmCode = null;
            this.mOldMethodAddress = NativeMethods.NULL;
            this.mNewMethodAddress = NativeMethods.NULL;
        }

        private byte[] GetHeadCode(IntPtr ptr)
        {
            byte[] buffer = new byte[5];
            Marshal.Copy(ptr, buffer, 0, 5);
            return buffer;
        }

        private byte[] ConvetToBinary(int num)
        {
            byte[] buffer = new byte[4];
            unsafe
            {
                fixed (byte* p = buffer)
                {
                    *(int*)p = num;
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
