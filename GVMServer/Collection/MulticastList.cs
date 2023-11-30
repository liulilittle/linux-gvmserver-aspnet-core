namespace GVMServer.Collection
{
    using System.Collections.Generic;

    public class MulticastList<T> : MulticastArray<T>
    {
        protected override ICollection<T> NewCollection()
        {
            return new List<T>();
        }
    }
}
