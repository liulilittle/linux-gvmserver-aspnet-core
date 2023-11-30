namespace GVMServer.AOP.Proxy
{
    public interface InvocationHandler
    {
        object InvokeMember(object obj, int metadataToken, string memberName, params object[] args);
    }
}
