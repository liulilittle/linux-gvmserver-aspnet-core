namespace GVMServer.Ns
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections;

    public unsafe class BufferSegment : IEnumerable
    {
        public static readonly byte[] Empty = new byte[0];
        public static readonly IntPtr Null = IntPtr.Zero;

        public virtual byte[] Buffer { get; }

        public virtual int Offset { get; }

        public virtual int Length { get; }

        public BufferSegment(byte[] buffer) : this(buffer, buffer?.Length ?? 0)
        {

        }

        public BufferSegment(byte[] buffer, int length) : this(buffer, 0, length)
        {

        }

        public BufferSegment(byte[] buffer, int offset, int length)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            this.Buffer = buffer ?? Empty;
            if ((offset + length) > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("The offset and length overflow the size of the buffer.");
            }
            this.Offset = offset;
            this.Length = length;
        }

        public virtual ArraySegment<byte> ToArraySegment()
        {
            return new ArraySegment<byte>(this.Buffer, this.Offset, this.Length);
        }

        public virtual byte[] ToArray()
        {
            return ToArraySegment().ToArray();
        }

        public void CopyTo(byte[] destination)
        {
            ToArraySegment().CopyTo(destination, 0);
        }

        public virtual void CopyTo(byte[] destination, int destinationIndex)
        {
            ToArraySegment().CopyTo(destination, destinationIndex);
        }

        public virtual void CopyTo(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            stream.Write(this.Buffer, this.Offset, this.Length);
        }

        public IntPtr UnsafeAddrOfPinnedArrayElement()
        {
            return UnsafeAddrOfPinnedArrayElement(null);
        }

        public virtual IntPtr UnsafeAddrOfPinnedArrayElement(Action<IntPtr> callback)
        {
            IntPtr ptr = IntPtr.Zero;
            var buffer = this.Buffer;
            fixed (byte* pinned = buffer)
            {
                if (pinned != null)
                {
                    int num = (this.Offset + this.Length);
                    if (buffer.Length >= num)
                    {
                        ptr = (IntPtr)(pinned + this.Offset);
                    }
                }
                callback?.Invoke(ptr);
            }
            return ptr;
        }

        public override string ToString()
        {
            string s = string.Empty;
            UnsafeAddrOfPinnedArrayElement((p) =>
            {
                if (p == null)
                    s = null;
                else
                    s = new string((sbyte*)p, 0, this.Length, Encoding.Default);
            });
            return s;
        }

        public virtual IEnumerator GetEnumerator()
        {
            int l = this.Length + this.Offset;
            byte[] buffer = this.Buffer;
            if ((this.Offset + this.Length) < buffer.Length)
            {
                for (int i = this.Offset; i < l; i++)
                {
                    yield return buffer[i];
                }
            }
        }
    }
}
