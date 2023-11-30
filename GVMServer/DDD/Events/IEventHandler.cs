namespace GVMServer.DDD.Events
{
    using System; 

    public interface IEventHandler<TEvent> where TEvent : IEvent
    {
        void Handle(TEvent e, Action<object> callback);
    }
}
