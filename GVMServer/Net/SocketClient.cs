namespace GVMServer.Net
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security;
    using GVMServer.Hooking;
    using GVMServer.W3Xiyou.Net;

    public class SocketClient : EventArgs, ISocketClient, IDisposable
    {
        private static readonly byte[] m_emptyBuf = new byte[0];

        private EventHandler<SocketMessage> m_onMessage;
        private EventHandler m_onOpen;
        private EventHandler m_onClose;
        private EventHandler m_onError;

        private readonly SocketListener m_listener;
        private bool m_available;
        private Socket m_socket;
        private readonly bool m_blocking;
        protected bool m_clientMode;
        private MessageReceiver m_receiver;
        private AsyncCallback m_recvAsyncCallback;
        private bool m_disposed = false;

        private class MessageReceiver
        {
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct FRAMEHEADER
            {
                public byte fk;
                public int len;
                public ushort cmd;
                public long seq;
                public long id;
            };

            public const int FRAMEHEADERKEY = 0x2A;
            public const int MSS = 1400; // MTU - PPP - 8(ICMP) - IPHDR - TCPHDR (JY: 1426)

            public bool existHdr;
            public int rdofs;
            public int len;
            public byte[] buffer = new byte[MSS];
            public MemoryStream stream = null;

            public ushort cmd;
            public long seq;
            public long id;

            public void Reset()
            {
                this.existHdr = false;
                this.rdofs = 0;
                this.len = 0;
                this.stream = null;

                this.cmd = 0;
                this.seq = 0;
                this.id = 0;
            }
        }

        [SecurityCritical]
        protected internal SocketClient(SocketListener listener, Socket socket) : this()
        {
            this.m_socket = socket ?? throw new ArgumentNullException(nameof(socket));
            this.m_listener = listener ?? throw new ArgumentNullException(nameof(listener));

            this.m_blocking = socket.Blocking;
            this.m_clientMode = false;
            this.m_available = socket.Connected;

            this.LocalEndPoint = socket.LocalEndPoint;
            this.RemoteEndPoint = socket.RemoteEndPoint;
        }

        public virtual SocketListener Listener => this.m_listener;

        [SecurityCritical]
        protected internal SocketClient()
        {
            this.m_clientMode = true;
            this.m_recvAsyncCallback = this.StartReceive;
            this.m_receiver = new MessageReceiver();
            this.m_receiver.Reset();
        }

        ~SocketClient()
        {
            this.Dispose();
        }

        public virtual void Dispose()
        {
            lock (this)
            {
                if (!m_disposed)
                {
                    this.Close();
                    m_recvAsyncCallback = null;
                    m_receiver.Reset();
                    m_receiver = null;
                    m_socket = null;
                    m_onOpen = null;
                    m_onClose = null;
                    m_onError = null;
                    m_onMessage = null;
                    m_disposed = true;
                }
            }
            GC.SuppressFinalize(this);
        }

        event EventHandler<SocketMessage> ISocketClient.OnMessage
        {
            add
            {
                m_onMessage += value;
            }
            remove
            {
                m_onMessage -= value;
            }
        }
        event EventHandler ISocketClient.OnOpen
        {
            add
            {
                m_onOpen += value;
            }
            remove
            {
                m_onOpen -= value;
            }
        }
        event EventHandler ISocketClient.OnClose
        {
            add
            {
                m_onClose += value;
            }
            remove
            {
                m_onClose -= value;
            }
        }
        event EventHandler ISocketClient.OnError
        {
            add
            {
                m_onError += value;
            }
            remove
            {
                m_onError -= value;
            }
        }

        public virtual bool Available
        {
            get
            {
                if (!this.m_available)
                {
                    return false;
                }
                Socket s = this.m_socket;
                return s != null && s.Connected;
            }
        }

        public virtual bool Blocking => this.m_blocking;

        public virtual EndPoint RemoteEndPoint { get; private set; }

        public virtual EndPoint LocalEndPoint { get; private set; }

        protected virtual void OnOpen(EventArgs e)
        {
            var onOpen = this.m_onOpen;
            onOpen?.Invoke(this, e);

            if (this is XiYouSdkClient socket)
            {
                EndPoint remoteEP = this.RemoteEndPoint;
                Console.WriteLine("{0}|open = {1}, identifier = {2}", DateTime.Now.ToString(), remoteEP?.ToString(), socket?.Identifier);
            }
        }

        protected virtual void OnClose(EventArgs e)
        {
            var onClose = this.m_onClose;
            onClose?.Invoke(this, e);

            if (this is XiYouSdkClient socket)
            {
                EndPoint remoteEP = this.RemoteEndPoint;
                Console.WriteLine("{0}|socket close = {1}, identifier = {2}", DateTime.Now.ToString(), remoteEP?.ToString(), socket?.Identifier);
            }
        }

        protected virtual void OnError(EventArgs e)
        {
            var onError = this.m_onError;
            onError?.Invoke(this, e);

            if (this is XiYouSdkClient socket)
            {
                EndPoint remoteEP = this.RemoteEndPoint;
                Console.WriteLine("{0}|socket error = {1}", DateTime.Now.ToString(), remoteEP?.ToString(), socket?.Identifier);
            }
        }

        protected virtual void OnMessage(SocketMessage e)
        {
            var onMessage = this.m_onMessage;
            onMessage?.Invoke(this, e);
        }

        protected void CloseOrAbort(bool aborting)
        {
            Socket socket = null;
            lock (this)
            {
                socket = this.m_socket;
                if (socket != null)
                {
                    this.m_socket = null;
                    this.m_available = false;
                    this.m_receiver.Reset();
                }
            }
            bool doEvt = false;
            if (socket != null)
            {
                doEvt = true;
                try
                {
                    socket.Shutdown(SocketShutdown.Send);
                    socket.Shutdown(SocketShutdown.Both);
                }
                catch (Exception) { }
                try
                {
                    socket.Close();
                    socket.Dispose();
                }
                catch (Exception) { }
            }
            if (doEvt)
            {
                if (aborting)
                {
                    this.OnError(EventArgs.Empty);
                }
                else
                {
                    this.OnClose(EventArgs.Empty);
                }
            }
        }

        public virtual void Abort()
        {
            this.CloseOrAbort(true);
        }

        private unsafe void StartReceive(IAsyncResult ar)
        {
            Socket socket = this.m_socket;
            SocketMessage message = null;
            int closeMode = 0;
            bool callRecvc = false;
            try
            {
                do
                {
                    if (socket == null || this.m_receiver == null)
                    {
                        break;
                    }
                    if (ar == null)
                    {
                        int len = sizeof(MessageReceiver.FRAMEHEADER);
                        if (this.m_receiver.existHdr)
                        {
                            int sp = this.m_receiver.len - this.m_receiver.rdofs;
                            if (sp <= 0)
                            {
                                closeMode = 1;
                                break;
                            }
                            if (sp > MessageReceiver.MSS)
                            {
                                sp = MessageReceiver.MSS;
                            }
                            len = sp;
                        }
                        try
                        {
                            socket.BeginReceive(this.m_receiver.buffer, 0, len, SocketFlags.None, this.m_recvAsyncCallback, null);
                        }
                        catch (Exception)
                        {
                            closeMode = 4;
                        }
                    }
                    else
                    {
                        SocketError socketError = SocketError.SocketError;
                        int len = -1;
                        try
                        {
                            len = socket.EndReceive(ar, out socketError);
                        }
                        catch (Exception)
                        {
                            closeMode = 5;
                            break;
                        }
                        if (len <= 0 || socketError != SocketError.Success)
                        {
                            closeMode = -1;
                            break;
                        }
                        if (!this.m_receiver.existHdr)
                        {
                            fixed (byte* pinned = this.m_receiver.buffer)
                            {
                                MessageReceiver.FRAMEHEADER* fkhdr = (MessageReceiver.FRAMEHEADER*)pinned;
                                if (len != sizeof(MessageReceiver.FRAMEHEADER) || fkhdr->fk != MessageReceiver.FRAMEHEADERKEY || fkhdr->len < 0)
                                {
                                    closeMode = 2;
                                }
                                else
                                {
                                    this.m_receiver.existHdr = true;
                                    this.m_receiver.rdofs = 0;
                                    this.m_receiver.len = fkhdr->len;

                                    if (fkhdr->len <= 0)
                                    {
                                        this.m_receiver.stream = null;
                                        this.m_receiver.Reset();

                                        message = this.CreateMessage(this, fkhdr->cmd, fkhdr->seq, fkhdr->id, m_emptyBuf, 0, 0);
                                    }
                                    else
                                    {
                                        this.m_receiver.seq = fkhdr->seq;
                                        this.m_receiver.id = fkhdr->id;
                                        this.m_receiver.cmd = fkhdr->cmd;

                                        this.m_receiver.stream = new MemoryStream(fkhdr->len);
                                    }

                                    callRecvc = true;
                                }
                            }
                        }
                        else
                        {
                            this.m_receiver.rdofs = unchecked(len + this.m_receiver.rdofs);
                            int sp = this.m_receiver.len - this.m_receiver.rdofs;
                            MemoryStream stream = null;
                            if (sp < 0 | (stream = this.m_receiver.stream) == null)
                            {
                                closeMode = 3;
                            }
                            else
                            {
                                stream.Write(this.m_receiver.buffer, 0, len);
                                if (sp == 0)
                                {
                                    byte[] payload = stream.GetBuffer();
                                    long payloadlen = stream.Position;
                                    message = this.CreateMessage(this, this.m_receiver.cmd, this.m_receiver.seq, this.m_receiver.id, payload, 0, payloadlen);
                                    using (stream)
                                    {
                                        this.m_receiver.Reset();
                                    }
                                }
                                callRecvc = true;
                            }
                        }
                    }
                } while (false);
            }
            catch (Exception)
            {
                message = default(SocketMessage);
            }
            if (closeMode == 0)
            {
                if (message != null)
                {
                    this.OnMessage(message);
                }
                if (callRecvc)
                {
                    this.StartReceive(null);
                }
            }
            else if (closeMode >= 1)
            {
                this.CloseOrAbort(true);
            }
            else if (closeMode == -1)
            {
                this.CloseOrAbort(false);
            }
        }

        protected virtual SocketMessage CreateMessage(ISocketClient socket, ushort commandId, long sequenceNo, long identifier, byte[] buffer, int offset, long length)
        {
            return new SocketMessage(socket, commandId, sequenceNo, identifier, buffer, offset, length);
        }

        public virtual void Close()
        {
            this.CloseOrAbort(false);
        }

        private bool ConnectSocket(EndPoint remoteEP)
        {
            if (remoteEP == null)
            {
                return false;
            }
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                this.m_socket = socket;
                socket.BeginConnect(remoteEP, (ar) =>
                {
                    bool aborting = false;
                    if (ar == null)
                    {
                        aborting = true;
                    }
                    else
                    {
                        try
                        {
                            socket.EndConnect(ar);

                            this.RemoteEndPoint = socket.RemoteEndPoint;
                            this.LocalEndPoint = socket.LocalEndPoint;
                            if (this.RemoteEndPoint == null)
                            {
                                this.CloseOrAbort(true);
                            }
                            else
                            {
                                this.m_available = true;
                                this.OnOpen(EventArgs.Empty);
                                this.m_recvAsyncCallback?.Invoke(null);
                            }
                        }
                        catch (Exception)
                        {
                            aborting = true;
                        }
                    }
                    if (aborting)
                    {
                        this.CloseOrAbort(true);
                    }
                }, null);
                return true;
            }
            catch (Exception)
            {
                if (socket != null)
                {
                    socket.Close();
                    socket.Dispose();
                }
            }
            return false;
        }

        public virtual bool Open(EndPoint remoteEP)
        {
            lock (this)
            {
                Socket socket = this.m_socket;
                if (socket == null)
                {
                    if (!this.m_clientMode)
                    {
                        return false;
                    }
                    return this.ConnectSocket(remoteEP);
                }
                else if (this.m_clientMode)
                {
                    return false;
                }
                if (!socket.Connected)
                {
                    this.CloseOrAbort(false);
                    return false;
                }
                else
                {
                    this.StartReceive(null);
                    return true;
                }
            }
        }

        public unsafe virtual bool Send(SocketMessage message)
        {
            if (message == null)
            {
                throw new ArgumentOutOfRangeException(nameof(message));
            }
            Socket socket = null;
            lock (this)
            {
                if (this.m_available)
                {
                    socket = this.m_socket;
                }
            }
            if (socket == null)
            {
                return false;
            }
            int count = Convert.ToInt32(message.GetLength());
            int offset = Convert.ToInt32(message.GetOffset());
            using (MemoryStream ms = new MemoryStream(count + sizeof(MessageReceiver.FRAMEHEADER)))
            {
                try
                {
                    byte[] header = new byte[sizeof(MessageReceiver.FRAMEHEADER)];
                    fixed (byte* pinned = header)
                    {
                        MessageReceiver.FRAMEHEADER* fkhdr = (MessageReceiver.FRAMEHEADER*)pinned;
                        fkhdr->len = count; // PAYLOAD_SIZE

                        fkhdr->id = message.Identifier; // 标识号
                        fkhdr->cmd = message.CommandId; // 命令号
                        fkhdr->seq = message.SequenceNo; // 流水号

                        fkhdr->fk = MessageReceiver.FRAMEHEADERKEY;
                    }
                    ms.Write(header, 0, header.Length);
                    if (count > 0)
                    {
                        ms.Write(message.GetBuffer(), offset, count);
                    }
                    socket.BeginSend(ms.GetBuffer(), 0, Convert.ToInt32(ms.Position), SocketFlags.None, (ar) =>
                    {
                        SocketError error = SocketError.Success;
                        try
                        {
                            socket.EndSend(ar, out error);
                        }
                        catch (Exception)
                        {
                            error = SocketError.SocketError;
                        }
                        if (error != SocketError.Success)
                        {
                            this.CloseOrAbort(true);
                        }
                    }, null);
                    return true;
                }
                catch (Exception)
                {
                    this.CloseOrAbort(true);
                    return false;
                }
            }
        }

        public static EthernetInterface[] GetAllEthernetInterfaces(Predicate<EthernetInterface> predicate = null)
        {
            var addresses = new Dictionary<string, EthernetInterface>();
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                byte[] buffer = ni.GetPhysicalAddress().GetAddressBytes();
                if (buffer == null || buffer.Length <= 0)
                {
                    continue;
                }

                string macAddress = BitConverter.ToString(buffer);
                if (string.IsNullOrEmpty(macAddress))
                {
                    continue;
                }

                if (addresses.ContainsKey(macAddress))
                {
                    continue;
                }

                EthernetInterface ethernet = new EthernetInterface
                {
                    NetworkInterface = ni,
                    MacAddress = macAddress
                };
                foreach (UnicastIPAddressInformation address in ni.GetIPProperties().UnicastAddresses)
                {
                    IPAddress addr = address.Address;
                    if (addr.AddressFamily != AddressFamily.InterNetwork || addr == IPAddress.Any)
                    {
                        continue;
                    }
                    ethernet.UnicastAddresse = addr;
                    break;
                }

                if (predicate == null || predicate(ethernet))
                {
                    addresses.Add(macAddress, ethernet);
                }
            }
            return addresses.Values.ToArray();
        }

        public static IPAddress[] GetActivityIPAddress()
        {
            List<IPAddress> results = new List<IPAddress>();
            ISet<IPAddress> sets = new HashSet<IPAddress>();
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType != NetworkInterfaceType.Ethernet && 
                    ni.NetworkInterfaceType != NetworkInterfaceType.Wireless80211 &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Ppp) // PPPOE宽带拨号
                {
                    continue;
                }

                if (ni.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                IPInterfaceProperties properties = ni.GetIPProperties();
                UnicastIPAddressInformationCollection addresses = properties.UnicastAddresses;
                foreach (UnicastIPAddressInformation address in addresses)
                {
                    IPAddress addr = address.Address;
                    if (addr.AddressFamily != AddressFamily.InterNetwork || addr == IPAddress.Any)
                    {
                        continue;
                    }

                    if (sets.Add(addr))
                    {
                        results.Add(addr);
                    }
                }
            }
            return results.ToArray();
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int setsockopt(int socket, int level, int option_name, IntPtr option_value, uint option_len);

        private const int SOL_SOCKET_OSX = 0xffff;
        private const int SO_REUSEADDR_OSX = 0x0004;
        private const int SOL_SOCKET_LINUX = 0x0001;
        private const int SO_REUSEADDR_LINUX = 0x0002;

        // Without setting SO_REUSEADDR on macOS and Linux, binding to a recently used endpoint can fail.
        // https://github.com/dotnet/corefx/issues/24562
        // https://github.com/aspnet/KestrelHttpServer/blob/1c0cf15b119c053d8db754fd8688c50655de8ce8/src/Kestrel.Transport.Sockets/SocketTransport.cs#L166-L196
        public static unsafe void EnableReuseAddress(Socket listenSocket)
        {
            if (listenSocket == null)
            {
                throw new ArgumentNullException(nameof(listenSocket));
            }
            int optionValue = 1;
            int setsockoptStatus = 0;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                setsockoptStatus = setsockopt(listenSocket.Handle.ToInt32(), SOL_SOCKET_LINUX, SO_REUSEADDR_LINUX,
                                              (IntPtr)(&optionValue), sizeof(int));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                setsockoptStatus = setsockopt(listenSocket.Handle.ToInt32(), SOL_SOCKET_OSX, SO_REUSEADDR_OSX,
                                              (IntPtr)(&optionValue), sizeof(int));
            }
            else
            {
                listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            }
            if (setsockoptStatus != 0)
            {
                throw new SystemException($"Setting SO_REUSEADDR failed with errno '{Marshal.GetLastWin32Error()}'.");
            }
        }

        private static readonly object _syncobj = new object();
        private static Interceptor _interceptor = null;

        [SecurityCritical]
        [SecuritySafeCritical]
        public static void Listen(Socket socket, int backlog)
        {
            Exception exception = null;
            if (socket == null)
            {
                exception = new NullReferenceException(nameof(socket));
            }
            else
            {
                if (backlog <= 0)
                {
                    backlog = 100;
                }
                exception = _interceptor.Invoke(() =>
                {
                    EnableReuseAddress(socket);
                    socket.Listen(backlog);
                });
            }
            if (exception != null)
            {
                throw exception;
            }
        }

        public static void Initialize()
        {
            lock (_syncobj)
            {
                if (_interceptor == null)
                {
                    _interceptor = new Interceptor(typeof(Socket).GetMethod("Listen"), typeof(SocketClient).GetMethod("Listen", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
                }
            }
        }
    }
}
