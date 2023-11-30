namespace XVM.Data.Mmx
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO.MemoryMappedFiles;
    using System.Threading;
    using System.Runtime.InteropServices;

    public unsafe class MmxDataTable
    {
        private readonly ConcurrentDictionary<int, MmxChunkTable> chunktables;
        private readonly ConcurrentDictionary<string, MmxChunkTable> mapchunks;
        private readonly LinkedList<MmxChunkTable> freechunks;
        private MemoryMappedViewAccessor mmva;
        private MemoryMappedFile mmf;
        private MmfDataTableHeader* datahdr;
        private readonly object syncobj = new object();

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct MmfDataTableHeader
        {
            public volatile int chunkscount;
        }

        public MmxDataTable(string path, string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (name.Length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(name));
            }
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (path.Length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(path));
            }
            this.Name = name;
            this.Path = path;
            this.chunktables = new ConcurrentDictionary<int, MmxChunkTable>();
            this.mapchunks = new ConcurrentDictionary<string, MmxChunkTable>();
            this.freechunks = new LinkedList<MmxChunkTable>();
            this.LoadAllChunk(path, name);
        }

        protected virtual int GetBlockSize()
        {
            return 10 * (1024 * 1024);
        }

        private void LoadAllChunk(string path, string name)
        {
            this.mmf = MmxChunkTable.CreateOrOpen(string.Format("{0}/mmx.data.{1}.cache", path, name), sizeof(MmfDataTableHeader));
            this.mmva = this.mmf.CreateViewAccessor();
            this.datahdr = (MmfDataTableHeader*)this.mmva.SafeMemoryMappedViewHandle.DangerousGetHandle();

            for (int chunkIndex = 1; chunkIndex <= this.datahdr->chunkscount; chunkIndex++)
            {
                string chunkName = string.Format("{0}/mmx.data.{1}.{2}.cache", path, this.Name, chunkIndex.ToString("d4"));
                if (!MmxChunkTable.Exists(chunkName))
                {
                    continue;
                }
                MmxChunkTable chunkTable = new MmxChunkTable(chunkName, this.GetBlockSize(), MmxApartment.MTA);
                this.chunktables.TryAdd(chunkIndex, chunkTable);
                this.freechunks.AddFirst(chunkTable);

                foreach (string key in chunkTable.GetAllKeys())
                {
                    this.mapchunks.AddOrUpdate(key, chunkTable, (ok, ov) => chunkTable);
                }
            }
        }

        public string Name
        {
            get;
            private set;
        }

        public string Path
        {
            get;
            private set;
        }

        private MmxChunkTable GetChunkTable(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }
            MmxChunkTable chunk;
            if (mapchunks.TryGetValue(key, out chunk))
            {
                return chunk;
            }
            return null;
        }

        private MmxChunkTable EnlargeChunkTable()
        {
            int chunkIndex = Interlocked.Increment(ref this.datahdr->chunkscount);
            string chunkName = string.Format("{0}/mmx.data.{1}.{2}.cache", this.Path, this.Name, chunkIndex.ToString("d4"));

            MmxChunkTable chunkTable = new MmxChunkTable(chunkName, this.GetBlockSize(), MmxApartment.MTA);
            this.chunktables.TryAdd(chunkIndex, chunkTable);
            this.freechunks.AddFirst(chunkTable);

            return chunkTable;
        }

        public bool ContainsKey(string key)
        {
            return this.GetChunkTable(key) != null;
        }

        public bool Remove(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }
            MmxChunkTable chunk = this.GetChunkTable(key);
            if (chunk == null)
            {
                return false;
            }
            if (chunk.Remove(key))
            {
                this.mapchunks.Remove(key, out MmxChunkTable chunkTable);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            lock (this.syncobj)
            {
                foreach (MmxChunkTable chunk in this.chunktables.Values)
                {
                    chunk.Clear();
                }
                this.mapchunks.Clear();
                this.freechunks.Clear();
                foreach (MmxChunkTable chunk in this.chunktables.Values)
                {
                    this.freechunks.AddLast(chunk);
                }
            }
        }

        public bool Set(string key, byte[] value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }
            lock (this.syncobj)
            {
                MmxChunkTable chunk = this.GetChunkTable(key);
                bool success = false;
                do
                {
                    if (chunk != null)
                    {
                        if (chunk.TrySet(key, value))
                        {
                            success = true;
                            break;
                        }
                        chunk.Remove(key);
                    }
                    LinkedListNode<MmxChunkTable> currentChunkNode = freechunks.First;
                    while (currentChunkNode != null)
                    {
                        LinkedListNode<MmxChunkTable> nextChunkNode = currentChunkNode.Next;
                        MmxChunkTable currentChunkTable = currentChunkNode.Value;
                        if (!currentChunkTable.TrySet(key, value))
                        {
                            freechunks.Remove(currentChunkNode);
                        }
                        else
                        {
                            chunk = currentChunkNode.Value;
                            success = true;
                            break;
                        }
                        currentChunkNode = nextChunkNode;
                    }
                    if (!success)
                    {
                        chunk = this.EnlargeChunkTable();
                        success = chunk.TrySet(key, value);
                    }
                } while (false);
                if (success)
                {
                    mapchunks.AddOrUpdate(key, chunk, (ok, ov) => chunk);
                }
                return success;
            }
        }

        public byte[] Get(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (key.Length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(key));
            }
            MmxChunkTable chunk = this.GetChunkTable(key);
            if (chunk == null)
            {
                throw new KeyNotFoundException(key);
            }
            return chunk.Get(key);
        }

        public IEnumerable<string> GetAllKeys()
        {
            return this.mapchunks.Keys;
        }

        public int Count
        {
            get
            {
                return this.mapchunks.Count;
            }
        }

        public bool TryGetValue(string key, out byte* buffer, out int count)
        {
            buffer = null;
            count = -1;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }
            MmxChunkTable chunk = this.GetChunkTable(key);
            if (chunk == null)
            {
                return false;
            }
            return chunk.TryGetValue(key, out buffer, out count);
        }

        public bool TryGetValue(string key, out byte[] buffer)
        {
            buffer = null;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }
            MmxChunkTable chunk = this.GetChunkTable(key);
            if (chunk == null)
            {
                return false;
            }
            return chunk.TryGetValue(key, out buffer);
        }
    }
}
