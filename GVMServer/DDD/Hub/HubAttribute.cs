namespace GVMServer.DDD.Hub
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class HubAttribute : Attribute
    {
        /// <summary>
        /// 集线器名称
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// 集线器条件一
        /// </summary>
        public int Condition1 { get; set; }

        /// <summary>
        /// 集线器条件二
        /// </summary>
        public int Condition2 { get; set; }

        /// <summary>
        /// 集线器条件三
        /// </summary>
        public int Condition3 { get; set; }

        /// <summary>
        /// 集线器条件四
        /// </summary>
        public string Condition4 { get; set; }

        /// <summary>
        /// 集线器条件五
        /// </summary>
        public string Condition5 { get; set; }

        /// <summary>
        /// 集线器条件六
        /// </summary>
        public string Condition6 { get; set; }
    }
}
