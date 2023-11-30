namespace GVMServer.Stage
{
    using System;
    using System.Collections.Concurrent;
    using GVMServer.Net;
    using GVMServer.Ns;
    using GVMServer.Ns.Net;
    using GVMServer.Ns.Net.Handler;
    using GVMServer.Ns.Net.Model;

    public class StageSocketHandler : SocketHandler
    {
        private readonly ConcurrentDictionary<ApplicationType, ConcurrentDictionary<Guid, LinkHeartbeat>> m_poSamplesTable =
            new ConcurrentDictionary<ApplicationType, ConcurrentDictionary<Guid, LinkHeartbeat>>();

        private ConcurrentDictionary<Guid, LinkHeartbeat> GetSamplesTable(ApplicationType applicationType)
        {
            lock (this.m_poSamplesTable)
            {
                if (!this.m_poSamplesTable.TryGetValue(applicationType, out ConcurrentDictionary<Guid, LinkHeartbeat> poSamplesTable) || poSamplesTable == null)
                {
                    this.m_poSamplesTable[applicationType] = poSamplesTable = new ConcurrentDictionary<Guid, LinkHeartbeat>();
                }
                return poSamplesTable;
            }
        }

        public virtual LinkHeartbeat GetLinkHeartbeat(ApplicationType applicationType, Guid id)
        {
            var poSamplesTable = GetSamplesTable(applicationType);
            lock (poSamplesTable)
            {
                poSamplesTable.TryGetValue(id, out LinkHeartbeat heartbeat);
                return heartbeat;
            }
        }

        public override bool ProcessLinkHeartbeat(NsSocket socket, long ackNo, LinkHeartbeat heartbeat)
        {
            if (!base.ProcessLinkHeartbeat(socket, ackNo, heartbeat))
            {
                return false;
            }

            var poSamplesTable = GetSamplesTable(socket.ApplicationType);
            lock (poSamplesTable)
            {
                poSamplesTable[socket.Id] = heartbeat;
            }
            return true;
        }

        public override bool ProcessAbort(NsSocket socket)
        {
            var poSamplesTable = GetSamplesTable(socket.ApplicationType);
            lock (poSamplesTable)
            {
                poSamplesTable.TryRemove(socket.Id, out LinkHeartbeat heartbeat);
            }
            return base.ProcessAbort(socket);
        }

        public override bool ProcessAccept(NsSocket socket)
        {
            return base.ProcessAccept(socket);
        }

        public override bool ProcessMessage(NsSocket socket, SocketMessage message)
        {
            return base.ProcessMessage(socket, message);
        }
    }
}
