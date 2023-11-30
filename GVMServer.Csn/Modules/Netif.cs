namespace GVMServer.Csn.Modules
{
    using System;
    using System.Collections.Generic;
    using GVMServer.DDD.Service;
    using GVMServer.Linq;
    using GVMServer.Ns;
    using GVMServer.Ns.Deployment;
    using GVMServer.Ns.Enum;
    using GVMServer.Ns.Functional;
    using GVMServer.Ns.Net;
    using GVMServer.Ns.Net.Mvh;

    public class Netif : IServiceBase
    {
        private readonly ISocket[] EmptySockets = new ISocket[0];

        public virtual ServerApplication GetServerApplication()
        {
            return ServiceObjectContainer.Get<ServerApplication>();
        }

        public virtual ApplicationChannel GetApplicationChannel()
        {
            var server = GetServerApplication();
            return server?.ApplicationChannel;
        }

        public virtual ISocketMvhHandler GetHandler(ApplicationType applicationType, Commands commands)
        {
            var channels = GetApplicationChannel();
            if (channels == null)
            {
                return null;
            }
            return channels.HandlerContainer.GetHandler(applicationType, commands);
        }

        public virtual SocketMvhHandlerAttribute GetAttribute(ApplicationType applicationType, Commands commands)
        {
            var channels = GetApplicationChannel();
            if (channels == null)
            {
                return null;
            }
            return channels.HandlerContainer.GetAttribute(applicationType, commands);
        }

        public virtual IEnumerable<ISocket> GetAllSockets(string platform, ApplicationType applicationType)
        {
            IEnumerable<ISocket> sockets = null;
            ApplicationChannel channels = GetApplicationChannel();
            if (channels != null)
            {
                sockets = string.IsNullOrEmpty(platform) ?
                    channels.GetAllSockets(applicationType) :
                    channels.GetAllSockets(applicationType, platform).Conversion(kv => kv.Value);
                if (applicationType == ApplicationType.ApplicationType_CrossServer) // 需要联合
                {
                    sockets = sockets.Union(
                        GetAllSockets(platform, ApplicationType.ApplicationType_GameServer).Filter(s => s.Credentials.BattleSuit == 1));
                }
            }
            return sockets ?? this.EmptySockets;
        }

        public class VirtualSocketNetif : ISocket
        {
            private readonly ISocketChannelManagement m_poChannelManagement = default(ISocketChannelManagement);
            private volatile Ns m_poCredentials = default(Ns);
            private readonly Guid m_stId = Guid.Empty;
            private readonly ApplicationType m_eApplicationType = ApplicationType.ApplicationType_Namespace;
            private readonly int m_iServerNo = 0;
            private readonly string m_szPlatform = string.Empty;

            protected internal VirtualSocketNetif(ISocketChannelManagement channelManagement, ApplicationType applicationType, string platform, int serverNo)
            {
                this.m_poChannelManagement = channelManagement ?? throw new ArgumentNullException(nameof(channelManagement));
                if (string.IsNullOrEmpty(platform))
                {
                    throw new ArgumentOutOfRangeException(nameof(platform));
                }
                this.m_iServerNo = serverNo;
                this.m_szPlatform = platform;
                this.m_eApplicationType = ApplicationType;
            }

            protected internal VirtualSocketNetif(ISocketChannelManagement channelManagement, ApplicationType applicationType, Guid guid)
            {
                this.m_poChannelManagement = channelManagement ?? throw new ArgumentNullException(nameof(channelManagement));
                this.m_stId = guid;
                this.m_eApplicationType = applicationType;
            }

            public virtual ISocketChannelManagement GetChannelManagement() => this.m_poChannelManagement;

            public ISocket GetSocket()
            {
                var channel = this.GetChannelManagement();
                if (this.m_stId == Guid.Empty)
                {
                    return channel.GetChannel(this.m_eApplicationType, this.m_szPlatform, this.m_iServerNo);
                }
                return channel.GetChannel(this.m_eApplicationType, this.m_stId);
            }

            public bool Available
            {
                get
                {
                    var socket = this.GetSocket();
                    if (socket == null)
                    {
                        return false;
                    }
                    return socket.Available;
                }
            }

            public Guid Id
            {
                get
                {
                    var credentials = this.Credentials;
                    if (credentials == null)
                    {
                        return this.m_stId;
                    }
                    return credentials.AssignNodeid;
                }
            }

            public string Platform
            {
                get
                {
                    var credentials = this.Credentials;
                    if (credentials == null)
                    {
                        return this.m_szPlatform;
                    }
                    return credentials.PlatformName;
                }
            }

            public int ServerNo
            {
                get
                {
                    var credentials = this.Credentials;
                    if (credentials == null)
                    {
                        return this.m_iServerNo;
                    }
                    return credentials.ServerNo;
                }
            }

            public ApplicationType ApplicationType
            {
                get
                {
                    var credentials = this.Credentials;
                    if (credentials == null)
                    {
                        return this.m_eApplicationType;
                    }
                    return credentials.ApplicationType;
                }
            }

            public Ns Credentials
            {
                get
                {
                    lock (this)
                    {
                        if (this.m_poCredentials == null)
                        {
                            var socket = this.GetSocket();
                            if (socket != null)
                            {
                                this.m_poCredentials = socket.Credentials;
                            }
                        }
                        return this.m_poCredentials;
                    }
                }
            }

            public void Close()
            {
                var socket = this.GetSocket();
                if (socket != null)
                {
                    socket.Close();
                }
            }

            public bool Send(Message message)
            {
                var socket = this.GetSocket();
                if (socket == null)
                {
                    return false;
                }
                return socket.Send(message);
            }
        }

        public ISocket GetVirtualSocket(ApplicationType applicationType, string platform, int serverNo) => this.GetVirtualSocket(this.GetApplicationChannel(), applicationType, platform, serverNo);

        public ISocket GetVirtualSocket(ApplicationType applicationType, Guid guid) => this.GetVirtualSocket(this.GetApplicationChannel(), applicationType, guid);

        public virtual ISocket GetVirtualSocket(ISocketChannelManagement management, ApplicationType applicationType, string platform, int serverNo)
        {
            return new VirtualSocketNetif(management, applicationType, platform, serverNo);
        }

        public virtual ISocket GetVirtualSocket(ISocketChannelManagement management, ApplicationType applicationType, Guid guid)
        {
            return new VirtualSocketNetif(management, applicationType, guid);
        }

        public virtual ISocket GetSocket(ApplicationType applicationType, string platform, int serverNo)
        {
            ISocket socket = null;
            ApplicationChannel channels = GetApplicationChannel();
            if (channels != null)
            {
                socket = channels.GetChannel(applicationType, platform, serverNo);
                if (socket == null)
                {
                    if (applicationType == ApplicationType.ApplicationType_CrossServer)
                    {
                        socket = channels.GetChannel(ApplicationType.ApplicationType_GameServer, platform, serverNo);
                    }
                }
            }
            return socket;
        }

        public int Send<T>(IEnumerable<ISocket> sockets, Commands commands, T messages, int ackTimeout = 0,
            int ackRetransmission = 0, RetransmissionEvent ackEvent = null, Commands ackCommands = SocketMvhHandlerAttribute.DefaultValue,
            ObjectSerializationMode? serializationMode = ObjectSerializationMode.Protobuf)
            => this.Send(sockets, commands, Message.NewId(), messages, ackTimeout, ackRetransmission, ackEvent, ackCommands, serializationMode);

        public virtual int Send<T>(IEnumerable<ISocket> sockets, Commands commands, long ackNo, T messages, int ackTimeout = 0,
            int ackRetransmission = 0, RetransmissionEvent ackEvent = null, Commands ackCommands = SocketMvhHandlerAttribute.DefaultValue,
            ObjectSerializationMode? serializationMode = ObjectSerializationMode.Protobuf)
        {
            int events = 0;
            do
            {
                ApplicationChannel channels = GetApplicationChannel();
                if (channels == null)
                {
                    break;
                }
                Message packet = new Message(messages.Serialize(serializationMode))
                {
                    CommandId = commands,
                    SequenceNo = ackNo,
                };
                foreach (ISocket socket in sockets)
                {
                    if (socket == null)
                    {
                        continue;
                    }

                    if (ackTimeout > 0 &&
                        ackRetransmission >= 0)
                    {
                        if (channels.Retransmission.AddRetransmission(socket.ApplicationType, socket.Id, packet, ackTimeout, ackRetransmission, ackEvent, ackCommands, out Exception exception))
                        {
                            events++;
                        }
                    }
                    else if (socket.Send(packet))
                    {
                        events++;
                    }
                }
            } while (false);
            return events;
        }

        public int Broadcast<T>(string platform, ApplicationType applicationType, Commands commands, T messages, int ackTimeout = 0,
            int ackRetransmission = 0, RetransmissionEvent ackEvent = null, Commands ackCommands = SocketMvhHandlerAttribute.DefaultValue)
            => this.Broadcast(platform, applicationType, commands, Message.NewId(), messages, ackTimeout, ackRetransmission, ackEvent, ackCommands);

        public virtual int Broadcast<T>(string platform, ApplicationType applicationType, Commands commands, long ackNo, T messages, int ackTimeout = 0,
            int ackRetransmission = 0, RetransmissionEvent ackEvent = null, Commands ackCommands = SocketMvhHandlerAttribute.DefaultValue)
        {
            int events = 0;
            do
            {
                ApplicationChannel channels = GetApplicationChannel();
                if (channels == null)
                {
                    break;
                }

                IEnumerable<ISocket> sockets = this.GetAllSockets(platform, applicationType);
                if (sockets.IsNullOrEmpty())
                {
                    break;
                }

                SocketMvhHandlerAttribute attributes = GetAttribute(applicationType, commands);
                events += this.Send(sockets, commands, ackNo, messages, ackTimeout, ackRetransmission, ackEvent, ackCommands, attributes?.SerializationMode);
            } while (false);
            return events;
        }
    }
}
