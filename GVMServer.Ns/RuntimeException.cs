namespace GVMServer.Ns
{
    using System;
    using GVMServer.Ns.Enum;

    public class RuntimeException : Exception
    {
        public Error Error { get; }

        public RuntimeException(Error error) => this.Error = error;

        public RuntimeException(Error error, string message) : base(message) => this.Error = error;
    }
}
