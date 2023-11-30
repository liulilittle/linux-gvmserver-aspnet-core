namespace XVM.Data.Mmx
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;

    unsafe class MmxChunkTable : IDisposable, IEnumerable<string>
    {
        private static readonly byte[] emptybuf = new byte[0];
        private MemoryMappedViewAccessor mmva;
        private MemoryMappedFile mmf;
        private void* bof;
        private void* eof;
        private bool disposed;
        private ChkHdr* chkhdr;
        private object syncobj = new object();
        private EventWaitable waitable;
        private IDictionary<string, IntPtr> chunkmaptable;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct RVA
        {
            private bool m_HasValue;
            private int m_Address;

            public int Address => this.m_Address;

            public bool HasValue
            {
                get => this.m_HasValue;
                set => this.m_HasValue = value;
            }

            public RVA(int address)
            {
                this.m_HasValue = true;
                this.m_Address = address;
            }

            public static RVA Null = new RVA();

            public override bool Equals(object obj)
            {
                if (obj == null)
                {
                    return this.m_HasValue;
                }
                if (obj is RVA)
                {
                    RVA right = (RVA)obj;
                    if (!right.m_HasValue && !this.m_HasValue)
                    {
                        return true;
                    }
                    else if ((!right.m_HasValue && this.m_HasValue) || (right.m_HasValue && !this.m_HasValue))
                    {
                        return false;
                    }
                    return right.Address == this.Address;
                }
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                int hashCode = -41434642;
                hashCode = hashCode * -1521134295 + base.GetHashCode();
                hashCode = hashCode * -1521134295 + this.m_HasValue.GetHashCode();
                hashCode = hashCode * -1521134295 + this.Address.GetHashCode();
                hashCode = hashCode * -1521134295 + this.HasValue.GetHashCode();
                return hashCode;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Chunk
        {
            private RVA right;
            private RVA left;
            private int keylen;
            private int datalen;

            public Chunk* RvaToVa(RVA rva)
            {
                if (!rva.HasValue)
                {
                    return null;
                }
                fixed (Chunk* p = &this)
                {
                    byte* m = (byte*)p;
                    m += rva.Address;
                    return (Chunk*)m;
                }
            }

            public RVA VaToRva(Chunk* va)
            {
                if (va == null)
                {
                    return RVA.Null;
                }
                fixed (Chunk* c = &this)
                {
                    byte* m1 = (byte*)va;
                    byte* m2 = (byte*)c;

                    long rva = unchecked((long)(m1 - m2));
                    int addr = Convert.ToInt32(rva);
                    return new RVA(addr);
                }
            }

            public Chunk* Left
            {
                get
                {
                    return this.RvaToVa(this.left);
                }
                set
                {
                    this.left = this.VaToRva(value);
                }
            }

            public Chunk* Right
            {
                get
                {
                    return this.RvaToVa(this.right);
                }
                set
                {
                    this.right = this.VaToRva(value);
                }
            }

            public string Key
            {
                get
                {
                    if (this.keylen < 0)
                    {
                        return null;
                    }
                    if (this.keylen == 0)
                    {
                        return string.Empty;
                    }
                    fixed (Chunk* p = &this)
                    {
                        sbyte* m = (sbyte*)p;
                        m += sizeof(Chunk);
                        return new string(m, 0, this.keylen);
                    }
                }
            }

            public int ValueLength
            {
                get
                {
                    return this.datalen;
                }
                set
                {
                    if (value < 0)
                    {
                        value = -1;
                    }
                    this.datalen = value;
                }
            }

            public int KeyLength
            {
                get
                {
                    return this.keylen;
                }
                set
                {
                    this.keylen = value;
                }
            }

            public bool IsEmpty
            {
                get
                {
                    return this.keylen <= 0 && this.datalen <= 0;
                }
            }

            public bool IsNullValue
            {
                get
                {
                    return this.datalen < 0;
                }
            }

            public Chunk* Next
            {
                get
                {
                    return this.RvaToVa(new RVA(this.Size));
                }
            }

            public int Size
            {
                get
                {
                    int rva = this.keylen;
                    rva += this.datalen;
                    rva += sizeof(Chunk);
                    return rva;
                }
            }

            public byte* GetKeyBuffer()
            {
                fixed (Chunk* c = &this)
                {
                    byte* p = (byte*)c;
                    return p + sizeof(Chunk);
                }
            }

            public byte* GetValueBuffer()
            {
                if (this.datalen <= 0)
                {
                    return null;
                }
                byte* p = this.GetKeyBuffer();
                long rva = this.keylen <= 0 ? 0 : this.keylen;
                return p + rva;
            }

            public void Reset()
            {
                this.keylen = -1;
                this.datalen = 0;
                this.left = RVA.Null;
                this.right = RVA.Null;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ChkHdr
        {
            private byte fk;
            private byte ver;
            private ushort rsv;
            private RVA last;
            private int rc;

            public Chunk* First
            {
                get
                {
                    fixed (ChkHdr* chkhdr = &this)
                    {
                        byte* p = (byte*)chkhdr;
                        p += sizeof(ChkHdr);
                        return (Chunk*)p;
                    }
                }
            }

            public Chunk* Last
            {
                get
                {
                    Chunk* chunk = this.First;
                    if (chunk == null)
                    {
                        return null;
                    }
                    return chunk->RvaToVa(this.last);
                }
                set
                {
                    Chunk* chunk = this.First;
                    if (chunk == null)
                    {
                        throw new InvalidOperationException(nameof(chunk));
                    }
                    this.last = chunk->VaToRva(value);
                }
            }

            public bool IsEmpty
            {
                get
                {
                    Chunk* chunk = this.First;
                    if (chunk == this.Last)
                    {
                        return chunk->IsEmpty;
                    }
                    return false;
                }
            }

            public int AddRef()
            {
                int n = Interlocked.Increment(ref this.rc);
                if (n < 0)
                {
                    n = 0;
                    Interlocked.Exchange(ref this.rc, n);
                }
                return n;
            }

            public int RelaseRef()
            {
                int n = Interlocked.Decrement(ref this.rc);
                if (n < 0)
                {
                    Interlocked.Exchange(ref this.rc, 0);
                }
                return n;
            }

            public int GetRef()
            {
                int n = Interlocked.CompareExchange(ref this.rc, 0, 0);
                if (n < 0)
                {
                    n = 0;
                    Interlocked.Exchange(ref this.rc, n);
                }
                return n;
            }

            public void Reset()
            {
                Chunk* chunk = this.First;
                if (chunk != null)
                {
                    chunk->Reset();
                }
                chunk = this.Last;
                if (chunk != null)
                {
                    chunk->Reset();
                    this.Last = null;
                }
                Interlocked.Exchange(ref this.rc, 0);
            }
        }

        public MmxChunkTable(string name, long capacity, MmxApartment apartment)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (name.Length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(name));
            }
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }
            if (!Enum.IsDefined(typeof(MmxApartment), apartment))
            {
                throw new ArgumentOutOfRangeException(nameof(apartment));
            }
            this.Apartment = apartment;
            this.mmf = CreateOrOpen(name, capacity);
            this.CreateOrOpenAllChunkViewAccessor(name, capacity);
        }

        public static MemoryMappedFile CreateOrOpen(string name, long capacity)
        {
            return MemoryMappedFile.CreateFromFile(name, FileMode.OpenOrCreate, name, capacity);
        }

        public static bool Exists(string name)
        {
            return File.Exists(name);
        }

        private void CreateOrOpenAllChunkViewAccessor(string name, long capacity)
        {
            this.mmva = this.mmf.CreateViewAccessor();
            SafeHandle handle = this.mmva.SafeMemoryMappedViewHandle;
            if (handle.IsClosed || handle.IsInvalid)
            {
                throw new IOException("Unable to open the view of shared memory correctly.");
            }
            this.bof = handle.DangerousGetHandle().ToPointer();
            this.eof = (byte*)this.bof + capacity;
            this.chkhdr = (ChkHdr*)this.bof;
            this.waitable = EventWaitable.CreateOrOpen(name);
            this.chunkmaptable = new Dictionary<string, IntPtr>();
            this.LoadAllChunkToMapTable();
        }

        private void LoadAllChunkToMapTable()
        {
            this.ReadWaitOne(() =>
            {
                this.WhileChunk((i) =>
                {
                    this.AddOrUpdateToChunkMap(i->Key, i);
                    return false;
                });
            });
        }

        public class EventWaitable : IDisposable
        {
            private EventWaitHandle readwaitable;
            private EventWaitHandle writewaitable;
            private bool disposed;

            public EventWaitHandle GetReadWaitable()
            {
                return this.readwaitable;
            }

            public EventWaitHandle GetWriteWaitable()
            {
                return this.writewaitable;
            }

            public string Name
            {
                get;
                private set;
            }

            public bool IsDisposed
            {
                get
                {
                    lock (this)
                    {
                        return this.disposed;
                    }
                }
            }

            private EventWaitable(string name)
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }
                if (name.Length <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(name));
                }
                this.Name = name;
                if (!EventWaitHandle.TryOpenExisting(name + ".rlock", out this.readwaitable))
                {
                    this.readwaitable = new EventWaitHandle(true, EventResetMode.ManualReset, name + ".rlock");
                }
                if (!EventWaitHandle.TryOpenExisting(name + ".wlock", out this.writewaitable))
                {
                    this.writewaitable = new EventWaitHandle(true, EventResetMode.AutoReset, name + ".wlock");
                }
            }

            ~EventWaitable()
            {
                this.Dispose();
            }

            public static EventWaitable CreateOrOpen(string name)
            {
                return new EventWaitable(name);
            }

            public void Dispose()
            {
                lock (this)
                {
                    if (!this.disposed)
                    {
                        this.disposed = true;
                        if (this.readwaitable != null)
                        {
                            this.readwaitable.Dispose();
                            this.readwaitable = null;
                        }
                        if (this.writewaitable != null)
                        {
                            this.writewaitable.Dispose();
                            this.writewaitable = null;
                        }
                    }
                }
                GC.SuppressFinalize(this);
            }
        }

        ~MmxChunkTable()
        {
            this.Dispose();
        }

        public MmxApartment Apartment
        {
            get;
            private set;
        }

        public int Count
        {
            get
            {
                if (this.chkhdr == null)
                {
                    return 0;
                }
                return this.chkhdr->GetRef();
            }
        }

        public bool IsDisposed
        {
            get
            {
                lock (this.syncobj)
                {
                    return this.disposed;
                }
            }
        }

        public virtual void Dispose()
        {
            lock (this.syncobj)
            {
                if (!this.disposed)
                {
                    this.disposed = true;
                    if (this.mmva != null)
                    {
                        this.mmva.Dispose();
                        this.mmva = null;
                    }
                    if (this.mmf != null)
                    {
                        this.mmf.Dispose();
                        this.mmf = null;
                    }
                    if (this.waitable != null)
                    {
                        this.waitable.Dispose();
                        this.waitable = null;
                    }
                    this.bof = null;
                    this.eof = null;
                    this.chkhdr = null;
                    lock (this.syncobj)
                    {
                        this.chunkmaptable.Clear();
                    }
                }
            }
            GC.SuppressFinalize(this);
        }

        private bool IsMemoryOutOfRange(Chunk* chunk, int keylen, int datalen)
        {
            if (keylen <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(keylen));
            }
            long allocater_size = keylen;
            if (datalen > 0)
            {
                allocater_size += datalen;
            }
            allocater_size += sizeof(Chunk);

            if (chunk != null && (chunk->ValueLength >= datalen && chunk->KeyLength >= keylen))
            {
                return false;
            }

            Chunk* chunk_last = this.chkhdr->Last;
            if (chunk_last == null)
            {
                chunk_last = this.chkhdr->First;
            }
            chunk_last = chunk_last->Next;

            long fasteffective_size = unchecked((long)((byte*)this.eof - ((byte*)chunk_last)));
            if (allocater_size > fasteffective_size)
            {
                return true;
            }
            return false;
        }

        protected Encoding GetEncoding()
        {
            return Encoding.Default;
        }

        private Chunk* FindChunk(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }
            Chunk* chunk = null;
            lock (this.chunkmaptable)
            {
                this.chunkmaptable.TryGetValue(key, out IntPtr p);
                chunk = (Chunk*)p;
                if (chunk != null)
                {
                    if (chunk->Key == key)
                    {
                        return chunk;
                    }
                    chunk = default(Chunk*);
                    if (this.Apartment != MmxApartment.MPA)
                    {
                        this.chunkmaptable.Remove(key);
                    }
                }
            }
            if (this.Apartment != MmxApartment.MPA)
            {
                return null;
            }
            this.WhileChunk((i) =>
            {
                string chunk_key = i->Key;
                {
                    this.AddOrUpdateToChunkMap(chunk_key, i);
                }
                if (key == chunk_key)
                {
                    chunk = i;
                    return true;
                }
                return false;
            });
            if (chunk != null)
            {
                this.AddOrUpdateToChunkMap(key, chunk);
            }
            return chunk;
        }

        public IEnumerable<string> GetAllKeys()
        {
            this.CheckOrThrowObjectException();
            ISet<string> out_ = new HashSet<string>();
            this.ReadWaitOne(() => this.WhileChunk((i) =>
            {
                string chunk_key = i->Key;
                if (this.Apartment == MmxApartment.MPA)
                {
                    this.AddOrUpdateToChunkMap(chunk_key, i);
                }
                out_.Add(chunk_key);
                return false;
            }));
            return out_;
        }

        public EventWaitable GetWaitable()
        {
            this.CheckOrThrowObjectException();
            return this.waitable;
        }

        private delegate bool WhileCallback(Chunk* chunk);

        private void WhileChunk(WhileCallback callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            Chunk* chunk = this.chkhdr->First;
            while (chunk != null)
            {
                if (chunk->IsEmpty)
                {
                    break;
                }
                if (callback(chunk))
                {
                    break;
                }
                chunk = chunk->Right;
            }
        }

        private void ReadWaitOne(Action callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            this.ReadWaitOne(() =>
            {
                callback();
                return false;
            });
        }

        private void WriteWaitOne(Action callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            this.WriteWaitOne(() =>
            {
                callback();
                return false;
            });
        }

        private T AcquireWaitable<T>(Func<T> callback, bool manual = false)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            if (this.waitable.GetReadWaitable().WaitOne() &&
                this.waitable.GetWriteWaitable().WaitOne())
            {
                if (this.chkhdr->GetRef() <= 0)
                {
                    this.ClearAllChunk();
                }
                T out_ = callback();
                if (!manual)
                {
                    this.waitable.GetWriteWaitable().Set();
                }
                return out_;
            }
            throw new InvalidOperationException("Cannot get to localTaken from the global domain.");
        }

        private T ReadWaitOne<T>(Func<T> callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            if (this.Apartment == MmxApartment.STA)
            {
                return callback();
            }
            return this.AcquireWaitable(() =>
            {
                if (!this.waitable.GetWriteWaitable().Set())
                {
                    throw new InvalidOperationException("Unable to emit/set signal to wlock.");
                }
                return callback();
            }, true);
        }

        private T WriteWaitOne<T>(Func<T> callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            if (this.Apartment == MmxApartment.STA)
            {
                return callback();
            }
            return this.AcquireWaitable(() =>
            {
                T out_ = callback();
                if (!this.waitable.GetReadWaitable().Set())
                {
                    throw new InvalidOperationException("Unable to emit/set signal to rlock.");
                }
                return out_;
            });
        }

        public byte[] Get(string key)
        {
            if (this.TryGetValue(key, out byte[] buffer))
            {
                return buffer;
            }
            throw new KeyNotFoundException("The value of the specified key cannot be found in the chunk table.");
        }

        public bool ContainsKey(string key)
        {
            this.CheckOrThrowObjectException();
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }
            return this.ReadWaitOne(() =>
            {
                Chunk* chunk = this.FindChunk(key);
                if (chunk == null)
                {
                    return false;
                }
                return true;
            });
        }

        public bool TryGetValue(string key, out byte* buffer, out int count)
        {
            this.CheckOrThrowObjectException();
            {
                byte* out_ = null;
                int len_ = 0;
                bool success = this.ReadWaitOne(() =>
                {
                    Chunk* chunk = this.FindChunk(key);
                    if (chunk == null)
                    {
                        return false;
                    }
                    len_ = chunk->ValueLength;
                    if (len_ > 0)
                    {
                        out_ = chunk->GetValueBuffer();
                    }
                    return true;
                });
                count = len_;
                buffer = out_;
                return success;
            }
        }

        public unsafe bool TryGetValue(string key, out byte[] buffer)
        {
            byte[] results = null;
            byte* dataptr = null;
            int datalen = -1;
            bool success = false;
            if ((success = this.TryGetValue(key, out dataptr, out datalen)))
            {
                if (datalen == 0)
                {
                    results = emptybuf;
                }
                else if (datalen > 0)
                {
                    results = new byte[datalen];
                    fixed (byte* pinned = results)
                    {
                        Buffer.MemoryCopy(dataptr, pinned, datalen, datalen);
                    }
                }
            }
            buffer = results;
            return success;
        }

        public bool TrySet(string key, byte[] buffer)
        {
            fixed (byte* p = buffer)
            {
                return this.TrySetChunk(key, p, (buffer == null ? -1 : buffer.Length));
            }
        }

        public bool TrySet(string key, byte[] buffer, int length)
        {
            return this.TrySet(key, buffer, 0, length);
        }

        public bool TrySet(string key, byte[] buffer, int offset, int length)
        {
            if (buffer == null && length != 0)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (buffer != null && (offset + length) > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            fixed (byte* pinned = buffer)
            {
                byte* data = pinned;
                if (data == null)
                {
                    length = -1;
                }
                return this.TrySetChunk(key, &data[offset], length);
            }
        }

        public bool TrySet(string key, byte* buffer, int length)
        {
            if (length == 0 && buffer == null)
            {
                length = -1;
            }
            if (buffer == null && length > 0)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            return this.TrySet(key, buffer, length);
        }

        private bool TrySetChunk(string key, byte* buffer, int length)
        {
            this.CheckOrThrowObjectException();
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (key.Length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(key));
            }
            Chunk* previous = null;
            byte[] nameid = this.GetEncoding().GetBytes(key);
            return this.WriteWaitOne(() =>
            {
                Chunk* chunk = this.FindChunk(key);
                int valuelength = unchecked(buffer == null ? -1 : length);
                if (this.IsMemoryOutOfRange(chunk, key.Length, valuelength))
                {
                    return false;
                }
                if (chunk == null)
                {
                    chunk = this.chkhdr->Last;
                }
                else if (chunk->ValueLength >= valuelength)
                {
                    chunk->ValueLength = valuelength;
                    if (valuelength > 0)
                    {
                        Buffer.MemoryCopy(buffer, chunk->GetValueBuffer(), valuelength, valuelength);
                    }
                    return true;
                }
                else
                {
                    this.RemoveChunk(chunk);
                    chunk = this.chkhdr->Last;
                }
                if (chunk == null)
                {
                    chunk = this.chkhdr->First;
                }
                else
                {
                    previous = chunk;
                    chunk = chunk->Next;
                }
                if (chunk != null)
                {
                    chunk->Reset();
                }
                chunk->ValueLength = valuelength;
                chunk->KeyLength = nameid.Length;
                fixed (byte* pinned = nameid)
                {
                    Buffer.MemoryCopy(pinned, chunk->GetKeyBuffer(), nameid.Length, nameid.Length);
                }
                if (valuelength > 0)
                {
                    Buffer.MemoryCopy(buffer, chunk->GetValueBuffer(), valuelength, valuelength);
                }
                do
                {
                    chunk->Left = previous; // link
                    chunk->Right = null;
                    if (previous != null)
                    {
                        previous->Right = chunk;
                    }
                    if (previous == this.chkhdr->Last)
                    {
                        this.chkhdr->Last = chunk;
                    }
                } while (false);
                this.chkhdr->AddRef();
                return this.AddOrUpdateToChunkMap(key, chunk);
            });
        }

        private bool AddOrUpdateToChunkMap(string key, Chunk* chunk)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }
            IntPtr p = unchecked((IntPtr)chunk);
            lock (this.chunkmaptable)
            {
                if (!this.chunkmaptable.TryAdd(key, p))
                {
                    this.chunkmaptable[key] = p;
                }
            }
            return true;
        }

        private bool RemoveChunk(Chunk* chunk)
        {
            if (chunk == null)
            {
                return false;
            }
            string key = chunk->Key;
            Chunk* left = chunk->Left;
            Chunk* right = chunk->Right;

            if (right != null)
            {
                right->Left = left;
            }
            if (left != null)
            {
                left->Right = right;
            }
            if (chunk == this.chkhdr->Last)
            {
                this.chkhdr->Last = left;
            }
            chunk->Reset();
            lock (this.chunkmaptable)
            {
                if (this.chkhdr->GetRef() <= 0 | this.chkhdr->RelaseRef() <= 0)
                {
                    this.chkhdr->Reset();
                    this.chunkmaptable.Clear();
                }
                this.chunkmaptable.Remove(key);
            }
            return true;
        }

        private void ClearAllChunk()
        {
            this.chkhdr->Reset();
            lock (this.chunkmaptable)
            {
                this.chunkmaptable.Clear();
            }
        }

        private void CheckOrThrowObjectException()
        {
            bool disposed = false;
            lock (this.syncobj)
            {
                disposed = this.disposed;
            }
            if (disposed)
            {
                throw new ObjectDisposedException("The object and the managed and unmanaged resources it holds have been freed.");
            }
        }

        public void Clear()
        {
            this.CheckOrThrowObjectException();
            this.WriteWaitOne(() => this.ClearAllChunk());
        }

        public bool Remove(string key)
        {
            this.CheckOrThrowObjectException();
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }
            return this.WriteWaitOne(() => this.RemoveChunk(this.FindChunk(key)));
        }

        public IEnumerator<string> GetEnumerator()
        {
            this.CheckOrThrowObjectException();
            if (this.Apartment == MmxApartment.MPA)
            {
                return this.GetAllKeys().GetEnumerator();
            }
            ICollection<string> keys = this.chunkmaptable.Keys;
            string[] results = new string[keys.Count];
            lock (this.chunkmaptable)
            {
                keys.CopyTo(results, 0);
            }
            return keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
