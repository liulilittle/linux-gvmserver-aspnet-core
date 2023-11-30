namespace GVMServer.Stage
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using GVMServer.Ns;
    using GVMServer.Ns.Enum;
    using GVMServer.Ns.Net;
    using GVMServer.Ns.Net.Handler;
    using GVMServer.Ns.Net.Model;
    using GVMServer.Ns.Net.Mvh;

    public class StageServerHandler : IServerHandler
    {
        private readonly ConcurrentDictionary<string, SocketMvhClient> m_poSocketTable = new ConcurrentDictionary<string, SocketMvhClient>();
        private readonly ConcurrentDictionary<string, SocketMvhClient> m_poNAT = new ConcurrentDictionary<string, SocketMvhClient>();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, SocketMvhClient>> m_poNATLinks =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, SocketMvhClient>>();
        private readonly object syncobj = new object();

        protected static string MeasureKey(ApplicationType applicationType, Guid id)
        {
            return $"{unchecked((int)applicationType)}.{id}";
        }

        protected virtual SocketMvhClient GetObjectChannel(ApplicationType applicationType, Guid id)
        {
            string channelKey = MeasureKey(applicationType, id);
            lock (this.syncobj)
            {
                m_poSocketTable.TryGetValue(channelKey, out SocketMvhClient poChannel);
                return poChannel;
            }
        }

        protected virtual SocketMvhClient GetReverseChannel(ApplicationType applicationType, Guid id)
        {
            string leftChannelKey = MeasureKey(applicationType, id);
            if (string.IsNullOrEmpty(leftChannelKey))
            {
                return null;
            }
            SocketMvhClient poRightChannel = null;
            lock (this.syncobj)
            {
                m_poNAT.TryGetValue(leftChannelKey, out poRightChannel);
            }
            return poRightChannel;
        }

        protected virtual bool CloseChannel(ApplicationType applicationType, Guid id)
        {
            string natChannelKey = MeasureKey(applicationType, id);
            lock (this.syncobj)
            {
                m_poNAT.TryRemove(natChannelKey, out SocketMvhClient poRightChannel);
                m_poSocketTable.TryRemove(natChannelKey, out SocketMvhClient poLeftChannel);
                if (poLeftChannel != null)
                {
                    poLeftChannel.Close();
                }

                if (m_poNATLinks.TryRemove(natChannelKey, out ConcurrentDictionary<string, SocketMvhClient> poNATLinks))
                {
                    foreach (SocketMvhClient socket in poNATLinks.Values)
                    {
                        if (socket == null)
                        {
                            continue;
                        }

                        CloseChannel(socket.ApplicationType, socket.Id);
                    }
                    poNATLinks.Clear();
                }
                return true;
            }
        }

        public ISocketChannelManagement ChannelManagement { get; }

        public ISocketHandler SocketHandler { get; }

        public StageServerHandler(ISocketChannelManagement poChannelManagement, ISocketHandler poSocketHandler)
        {
            this.ChannelManagement = poChannelManagement ?? throw new ArgumentNullException(nameof(poChannelManagement));
            this.SocketHandler = poSocketHandler ?? throw new ArgumentNullException(nameof(poSocketHandler));
        }

        protected virtual SocketMvhClient AllocationPressureSocket(ApplicationType applicationType)
        {
            var sockets = this.ChannelManagement.GetAllSockets(applicationType);
            if (sockets == null)
            {
                return null;
            }

            StageSocketHandler handler = this.SocketHandler as StageSocketHandler;
            if (handler == null)
            {
                foreach (ISocket socket in sockets)
                {
                    var poPressureSocket = socket as SocketMvhClient;
                    if (poPressureSocket == null)
                    {
                        continue;
                    }

                    if (!poPressureSocket.Available)
                    {
                        continue;
                    }

                    return poPressureSocket;
                }
                return null;
            }
            else
            {
                SocketMvhClient poAllocationSockets = null;
                LinkHeartbeat poAllocationLinkHeartbeat = null;
                foreach (ISocket socket in sockets)
                {
                    var poPressureSocket = socket as SocketMvhClient;
                    if (poPressureSocket == null)
                    {
                        continue;
                    }

                    LinkHeartbeat poLinkHeartbeat = handler.GetLinkHeartbeat(poPressureSocket.ApplicationType, poPressureSocket.Id);
                    if (poLinkHeartbeat == null)
                    {
                        poAllocationSockets = poPressureSocket;
                        poAllocationLinkHeartbeat = poLinkHeartbeat;
                        break;
                    }

                    if (poAllocationSockets == null || poAllocationLinkHeartbeat.CPULOAD > poLinkHeartbeat.CPULOAD)
                    {
                        poAllocationSockets = poPressureSocket;
                        poAllocationLinkHeartbeat = poLinkHeartbeat;
                    }
                }
                return poAllocationSockets;
            }
        }

        protected virtual int BroadcastAll(ApplicationType applicationType, Message message)
        {
            var sockets = this.ChannelManagement.GetAllSockets(applicationType);
            if (sockets == null)
            {
                return 0; 
            }

            int counts = 0;
            foreach (ISocket socket in sockets)
            {
                if (socket == null)
                {
                    continue;
                }

                if (!socket.Send(message))
                {
                    continue;
                }

                counts++;
            }
            return counts;
        }

        public virtual bool ProcessAccept(SocketMvhClient socket)
        {
            string natChannelKey = MeasureKey(socket.ApplicationType, socket.Id);
            if (string.IsNullOrEmpty(natChannelKey))
            {
                return false;
            }

            bool kBroadcastAcceptSocket = false;
            lock (this.syncobj)
            {
                if (socket.ApplicationType == ApplicationType.ApplicationType_GameServer || socket.ApplicationType == ApplicationType.ApplicationType_CrossServer)
                {
                    SocketMvhClient poPressureSocket = AllocationPressureSocket(ApplicationType.ApplicationType_ComputeNode);
                    if (poPressureSocket == null)
                    {
                        return false;
                    }

                    kBroadcastAcceptSocket = true;
                    m_poNAT[natChannelKey] = poPressureSocket;

                    string ownerChannelKey = MeasureKey(poPressureSocket.ApplicationType, poPressureSocket.Id);
                    if (!m_poNATLinks.TryGetValue(ownerChannelKey, out ConcurrentDictionary<string, SocketMvhClient> poNATLinks))
                    {
                        m_poNATLinks[ownerChannelKey] = poNATLinks = new ConcurrentDictionary<string, SocketMvhClient>();
                    }
                    poNATLinks[natChannelKey] = socket;
                }

                m_poSocketTable[natChannelKey] = socket;
            }

            if (kBroadcastAcceptSocket)
            {
                AcceptSocketMessage poAcceptSocketMessage = new AcceptSocketMessage()
                {
                    ApplicationType = socket.ApplicationType,
                    Credentials = socket.Credentials,
                    Id = socket.Id
                };
                BroadcastAll(ApplicationType.ApplicationType_ComputeNode, new Message(poAcceptSocketMessage.Serialize().ToBufferSegment())
                {
                    SequenceNo = Message.NewId(),
                    CommandId = Commands.Commands_AcceptSocket,
                });
            }
            else if (socket.ApplicationType == ApplicationType.ApplicationType_ComputeNode)
            {
                SynchronousAcceptSocketsList(ApplicationType.ApplicationType_GameServer, new[] { socket });
                SynchronousAcceptSocketsList(ApplicationType.ApplicationType_CrossServer, new[] { socket });
            }
            return true;
        }

        protected virtual int SynchronousAcceptSocketsList(ApplicationType applicationType, IEnumerable<ISocket> sockets)
        {
            if (sockets == null)
            {
                return 0;
            }

            AcceptAllSocketMessage poAcceptAllSocketMessage = new AcceptAllSocketMessage();
            poAcceptAllSocketMessage.AcceptSockets = new List<AcceptSocketMessage>();

            var poAllSockets = this.ChannelManagement.GetAllSockets(applicationType);
            if (poAllSockets != null)
            {
                foreach (ISocket poSocket in poAllSockets)
                {
                    if (poSocket is SocketMvhClient poChannel)
                    {
                        AcceptSocketMessage poAcceptSocketMessage = new AcceptSocketMessage()
                        {
                            ApplicationType = poChannel.ApplicationType,
                            Id = poChannel.Id,
                            Credentials = poChannel.Credentials
                        };
                        poAcceptAllSocketMessage.AcceptSockets.Add(poAcceptSocketMessage);
                    }
                }
            }

            Message message = new Message(poAcceptAllSocketMessage.Serialize().ToBufferSegment())
            {
                SequenceNo = Message.NewId(),
                CommandId = Commands.Commands_AcceptAllSocket,
            };

            int counts = 0;
            foreach (ISocket poChannel in sockets)
            {
                if (poChannel == null)
                {
                    continue;
                }

                if (!poChannel.Send(message))
                {
                    continue;
                }

                counts++;
            }
            return counts;
        }

        public virtual bool ProcessClose(SocketMvhClient socket)
        {
            CloseChannel(applicationType: socket.ApplicationType, id: socket.Id);
            if (socket.ApplicationType == ApplicationType.ApplicationType_GameServer || socket.ApplicationType == ApplicationType.ApplicationType_CrossServer)
            {
                AbortSocketMessage poAbortSocketMessage = new AbortSocketMessage()
                {
                    Id = socket.Id,
                    ApplicationType = socket.ApplicationType,
                };
                BroadcastAll(ApplicationType.ApplicationType_ComputeNode, new Message(poAbortSocketMessage.Serialize().ToBufferSegment())
                {
                    SequenceNo = Message.NewId(),
                    CommandId = Commands.Commands_AbortSocket,
                });
            }
            return true;
        }

        public virtual bool ProcessMessage(SocketMvhClient socket, Message message)
        {
            if (this.ProcessRoutingMessage(message))
            {
                return true;
            }
            if (message.CommandId == Commands.Commands_Transitroute)
            {
                TransitrouteMessage poTransitrouteMessage = TransitrouteMessage.From(message.Payload);
                do
                {
                    if (poTransitrouteMessage == null)
                    {
                        break;
                    }
                    SocketMvhClient sockets = this.GetObjectChannel(poTransitrouteMessage.ApplicationType, poTransitrouteMessage.Id);
                    if (sockets == null)
                    {
                        break;
                    }
                    sockets.Send(poTransitrouteMessage.Message);
                } while (false);
            }
            else
            {
                SocketMvhClient poReverseChannel = this.GetReverseChannel(socket.ApplicationType, socket.Id);
                if (poReverseChannel != null)
                {
                    TransitrouteMessage poTransitrouteMessage = new TransitrouteMessage()
                    {
                        Message = message,
                        Id = socket.Id,
                        ApplicationType = socket.ApplicationType,
                    };
                    poReverseChannel.Send(new Message(poTransitrouteMessage.ToArray().ToBufferSegment())
                    {
                        SequenceNo = Message.NewId(),
                        CommandId = Commands.Commands_Transitroute,
                    });
                }
            }
            return true;
        }

        private bool ProcessRoutingMessage(Message message)
        {
            if (message.CommandId != Commands.Commands_RoutingMessage)
            {
                return false;
            }

            RoutingMessage poRoutingMessage = RoutingMessage.From(message.Payload);
            if (poRoutingMessage == null)
            {
                return false;
            }

            ISocket poChannel = this.ChannelManagement?.GetChannel(poRoutingMessage.PeerApplicationType, poRoutingMessage.PeerPlatform, poRoutingMessage.PeekServerNo);
            if (poChannel == null)
            {
                return false;
            }

            return poChannel.Send(message);
        }
    }
}
