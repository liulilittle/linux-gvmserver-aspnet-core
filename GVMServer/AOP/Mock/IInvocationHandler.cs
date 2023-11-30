namespace GVMServer.AOP.Mock
{
    public interface IInvocationHandler
    {
        object InvokeMember(object obj, int method, string name, int module, params object[] args);
    }
}
