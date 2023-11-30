namespace GVMServer.Ns.Net.Mvh
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Ns = GVMServer.Ns.Functional.Ns;

    public class SocketMvhClient : EventArgs, IEnumerable<ISocket>, ISocket
    {
        private ConcurrentDictionary<ISocket, ISocket> m_oSocketTable = new ConcurrentDictionary<ISocket, ISocket>();
        /// <summary>
        /// 消息到达事件
        /// </summary>
        public event EventHandler<Message> Message;
        /// <summary>
        /// 套接字Mvh应用
        /// </summary>
        public SocketMvhApplication Application { get; }
        /// <summary>
        /// 当前客户端的实例Id
        /// </summary>
        public Guid Id { get; }
        /// <summary>
        /// 当前客户端的实例类型
        /// </summary>
        public ApplicationType ApplicationType { get; }
        /// <summary>
        /// 凭证信息
        /// </summary>
        public Ns Credentials { get; }
        /// <summary>
        /// 可用通道数量
        /// </summary>
        public int AvailableChannels => m_oSocketTable.Count;
        /// <summary>
        /// 通道是否可用
        /// </summary>
        public bool Available => this.AvailableChannels > 0;
        /// <summary>
        /// 添加一个通道
        /// </summary>
        /// <param name="socket">套接字通道</param>
        public virtual bool AddChannel(ISocket socket)
        {
            if (socket == null)
            {
                return false;
            }
            return m_oSocketTable.TryAdd(socket, socket);
        }
        /// <summary>
        /// 关闭一个通道
        /// </summary>
        /// <param name="socket"></param>
        public virtual bool CloseChannel(ISocket socket)
        {
            if (socket == null)
            {
                return false;
            }
            bool close = m_oSocketTable.TryRemove(socket, out ISocket ns);
            if (ns != null)
            {
                ns.Close();
            }
            return close;
        }
        /// <summary>
        /// 关闭全部通道
        /// </summary>
        public virtual void Close()
        {
            foreach (ISocket socket in 
                m_oSocketTable.Values)
            {
                socket?.Close();
            }
            m_oSocketTable.Clear();
        }
        /// <summary>
        /// 推送消息到客户端
        /// </summary>
        /// <param name="message">消息正文</param>
        public virtual bool Send(Message message)
        {
            if (message == null)
            {
                return false;
            }

            foreach (ISocket socket in m_oSocketTable.Values)
            {
                if (!socket.Available)
                {
                    continue;
                }

                if (!socket.Send(message))
                {
                    continue;
                }

                return true;
            }
            return false;
        }
        /// <summary>
        /// 获取当前客户端的通道迭代器
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        /// <summary>
        /// 获取当前客户端的通道迭代器
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerator<ISocket> GetEnumerator()
        {
            return this.m_oSocketTable.Values.GetEnumerator();
        }
        /// <summary>
        /// 请求消息达到
        /// </summary>
        /// <param name="message">请求的消息正文</param>
        protected internal virtual void OnMessage(Message message)
        {
            var events = this.Message;
            events?.Invoke(this, message);
        }
        /// <summary>
        /// 实例化一个Mvh的套接字客户端
        /// </summary>
        public SocketMvhClient(SocketMvhApplication application, ApplicationType applicationType, Guid id, Ns credentials)
        {
            this.Application = application ?? throw new ArgumentNullException(nameof(application));
            this.Id = id;
            this.ApplicationType = applicationType;
            this.Credentials = credentials ?? throw new ArgumentOutOfRangeException(nameof(credentials));
        }
    }
}
