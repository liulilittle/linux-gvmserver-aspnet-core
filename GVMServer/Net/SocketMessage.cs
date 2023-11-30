namespace GVMServer.Net
{
    using System;
    using System.IO;
    using System.Threading;

    public class SocketMessage : EventArgs
    {
        private static long m_AutoIncrSeqNo = 1;
        private ISocketClient m_Socket;
        private readonly byte[] m_Buffer;
        private readonly long m_Offset;
        private readonly long m_Length;
        private readonly ushort m_CommandId;
        private readonly long m_SequenceNo;
        private readonly long m_Identifier;

        public virtual ushort CommandId => this.m_CommandId;

        public virtual long SequenceNo => this.m_SequenceNo;

        public virtual long Identifier => this.m_Identifier;

        public virtual long GetLength() => this.m_Length;

        public virtual long GetOffset() => this.m_Offset;

        public virtual byte[] GetBuffer() => this.m_Buffer;

        public ISocketClient GetClient() => this.m_Socket;

        public static long NewId()
        {
            long ackid = 0;
            while (0 == (ackid = Interlocked.Increment(ref m_AutoIncrSeqNo))) ;
            return ackid;
        }

        public SocketMessage(ISocketClient socket, ushort commandId, long sequenceNo, long identifier, byte[] buffer, int offset, long length)
        {
            this.m_Socket = socket;
            this.m_Offset = offset;
            this.m_Length = length;
            this.m_Buffer = buffer;
            this.m_Identifier = identifier;
            this.m_SequenceNo = sequenceNo;
            this.m_CommandId = commandId;
        }
    }
}
