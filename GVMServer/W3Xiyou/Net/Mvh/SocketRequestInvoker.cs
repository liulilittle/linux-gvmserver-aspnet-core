namespace GVMServer.W3Xiyou.Net.Mvh
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using GVMServer.Net;
    using GVMServer.Serialization;

    public enum SocketReuqestInvokerError
    {
        Success,
        Error,
        Timeout,
    }

    public class SocketRequestInvoker
    {
        private class SocketInvokerContext
        {
            public long socketid;
            public int ackcount;
            public DateTime acktime;
            public SocketMessage message;
            public string platform;
            public SocketHandlerAttribute attribute;
            public Action<SocketReuqestInvokerError, SocketMessage> callback;
        }

        private readonly ConcurrentDictionary<long, SocketInvokerContext> invokers = new ConcurrentDictionary<long, SocketInvokerContext>();

        public SocketMvhApplication Application { get; private set; }

        protected internal SocketRequestInvoker(SocketMvhApplication mvh)
        {
            this.Application = mvh ?? throw new ArgumentNullException(nameof(mvh));
        }

        protected internal virtual void DoEvents(SocketMessage e)
        {
            if (e == null)
            {
                foreach (KeyValuePair<long, SocketInvokerContext> kv in invokers)
                {
                    SocketInvokerContext context = kv.Value;
                    long key = kv.Key;
                    if (context == null)
                    {
                        invokers.TryRemove(key, out SocketInvokerContext sicv);
                    }
                    else
                    {
                        var attribute = context.attribute;
                        var nowtime = DateTime.Now;
                        var tsdiff = nowtime - context.acktime;
                        if (tsdiff.TotalMilliseconds > attribute.AckTimeout)
                        {
                            if (context.ackcount >= attribute.RetryAckCount ||
                                tsdiff.TotalMilliseconds >= (attribute.AckTimeout * attribute.RetryAckCount))
                            {
                                invokers.TryRemove(key, out SocketInvokerContext sicv);
                                context.callback?.Invoke(SocketReuqestInvokerError.Timeout, null);
                            }
                            else
                            {
                                var socket = this.Application.GetClient(context.platform, context.socketid);
                                if (socket != null && socket.Send(context.message))
                                {
                                    context.acktime = nowtime;
                                    context.ackcount++;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                SocketInvokerContext context = null;
                lock (this.invokers)
                {
                    if (this.invokers.TryGetValue(e.SequenceNo, out SocketInvokerContext c1))
                    {
                        if (c1 != null && c1.socketid == this.Application.GetIdentifier(e.GetClient()))
                        {
                            context = c1;
                            invokers.TryRemove(e.SequenceNo, out SocketInvokerContext c2);
                        }
                    }
                }
                context?.callback(SocketReuqestInvokerError.Success, e);
            }
        }

        public bool InvokeAsync<T>(ISocketClient socket, ushort commands, object message, Action<SocketReuqestInvokerError, T> callback) => this.InvokeAsync(socket, commands, this.SerializeObject(message), callback);

        public bool InvokeAsync<T>(ISocketClient socket, ushort commands, byte[] buffer, Action<SocketReuqestInvokerError, T> callback) => this.InvokeAsync(socket, commands, buffer, (buffer == null ? 0 : buffer.Length), callback);

        public bool InvokeAsync<T>(ISocketClient socket, ushort commands, byte[] buffer, int length, Action<SocketReuqestInvokerError, T> callback) => this.InvokeAsync(socket, commands, buffer, 0, length, callback);

        protected virtual byte[] SerializeObject(object o)
        {
            return BinaryFormatter.Serialize(o, false);
        }

        protected virtual T DeserializeObject<T>(Stream stream)
        {
            return BinaryFormatter.Deserialize<T>(stream, false); ;
        }

        public bool InvokeAsync<T>(ISocketClient socket, ushort commandId, byte[] buffer, int offset, int length, Action<SocketReuqestInvokerError, T> callback)
        {
            if (socket == null)
            {
                return false;
            }
            XiYouSdkClient client = socket as XiYouSdkClient;
            return this.InvokeAsync<T>(client?.Platform, this.Application.GetIdentifier(socket), commandId, buffer, offset, length, callback);
        }

        public bool InvokeAsync<T>(string platform, long identifier, ushort commands, object message, Action<SocketReuqestInvokerError, T> callback) => this.InvokeAsync(platform, identifier, commands, this.SerializeObject(message), callback);

        public bool InvokeAsync<T>(string platform, long identifier, ushort commands, byte[] buffer, Action<SocketReuqestInvokerError, T> callback) => this.InvokeAsync(platform, identifier, commands, buffer, (buffer == null ? 0 : buffer.Length), callback);

        public bool InvokeAsync<T>(string platform, long identifier, ushort commands, byte[] buffer, int length, Action<SocketReuqestInvokerError, T> callback) => this.InvokeAsync(platform, identifier, commands, buffer, 0, length, callback);

        public virtual bool InvokeAsync<T>(string platform, long identifier, ushort commandId, byte[] buffer, int offset, int length, Action<SocketReuqestInvokerError, T> callback)
        {
            if (buffer == null && (length + offset) > 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            if (buffer != null && (length + offset) > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            SocketHandlerAttribute attribute = this.Application.GetAttribute(commandId);
            if (attribute == null)
            {
                throw new ArgumentOutOfRangeException(nameof(commandId));
            }
            long sequenceid = 0;
            do
                sequenceid = SocketMessage.NewId();
            while (this.invokers.ContainsKey(sequenceid));
            SocketMessage message = new SocketMessage(null, commandId, sequenceid, identifier, buffer, offset, length);

            ISocketClient socket = this.Application.GetClient(platform, identifier);
            if (socket == null || !socket.Send(message))
            {
                if (attribute.RetryAckCount <= 0)
                {
                    return false;
                }
            }
            SocketInvokerContext context = new SocketInvokerContext();
            context.platform = platform;
            context.message = message;
            context.acktime = DateTime.Now;
            context.attribute = attribute;
            context.ackcount = 1;
            context.socketid = identifier;

            if (callback != null)
            {
                context.callback = new Action<SocketReuqestInvokerError, SocketMessage>((errno, e) =>
                {
                    T result = default(T);
                    if (errno == SocketReuqestInvokerError.Success)
                    {
                        try
                        {
                            if (e.GetBuffer() == null && (e.GetOffset() + e.GetLength()) > 0 ||
                                 e.GetBuffer() != null && (e.GetOffset() + e.GetLength()) > e.GetBuffer().Length)
                            {
                                errno = SocketReuqestInvokerError.Error;
                            }
                            else
                            {
                                int count = Convert.ToInt32(e.GetLength());
                                int ofs = Convert.ToInt32(e.GetOffset());
                                if (typeof(string) == typeof(T))
                                {
                                    result = (T)(object)BinaryFormatter.DefaultEncoding.GetString(e.GetBuffer(), ofs, count);
                                }
                                else if (typeof(byte[]) == typeof(T))
                                {
                                    byte[] contents = e.GetBuffer();
                                    if (contents.Length > (ofs + count))
                                    {
                                        contents = new byte[count];
                                        Buffer.BlockCopy(e.GetBuffer(), ofs, contents, 0, count);
                                    }
                                    result = (T)(object)contents;
                                }
                                else
                                {
                                    using (MemoryStream ms = new MemoryStream(e.GetBuffer(), ofs, count))
                                    {
                                        result = this.DeserializeObject<T>(ms);
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                            errno = SocketReuqestInvokerError.Error;
                        }
                    }
                    callback(errno, result);
                });
            }

            return this.invokers.TryAdd(sequenceid, context);
        }
    }
}