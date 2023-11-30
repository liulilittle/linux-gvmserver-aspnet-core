namespace GVMServer.Ns.Net.Mvh
{
    using System;
    using System.Diagnostics;
    using GVMServer.Ns.Enum;

    /// <summary>
    /// 对象序列化模式
    /// </summary>
    public enum ObjectSerializationMode
    {
        BinaryFormatter,            // 二进制格式化
        Protobuf,                   // 协议缓冲序列
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class SocketMvhHandlerAttribute : Attribute
    {
        public static readonly ApplicationType[] DefaultApplicationType = new ApplicationType[] { Ns.ApplicationType.ApplicationType_Namespace };
        public const Commands DefaultValue = (Commands)unchecked((ushort)~0);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _ackCommandId = ~0;
        /// <summary>
        /// 序列化模式
        /// </summary>
        public ObjectSerializationMode SerializationMode { get; set; } = ObjectSerializationMode.BinaryFormatter;
        /// <summary>
        /// 命令代号
        /// </summary>
        public Commands CommandId { get; set; } = Commands.Commands_Authentication;
        /// <summary>
        /// 确认应答的命令代号
        /// </summary>
        public Commands AckCommandId
        {
            get
            {
                if (this._ackCommandId < 0)
                    return this.CommandId;
                return (Commands)this._ackCommandId;
            }
            set
            {
                this._ackCommandId = (int)value;
            }
        }
        /// <summary>
        /// 应用类型
        /// </summary>
        public ApplicationType[] ApplicationType { get; set; } = DefaultApplicationType;
    }
}
