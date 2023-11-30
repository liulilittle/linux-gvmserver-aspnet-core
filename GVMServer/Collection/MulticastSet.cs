namespace GVMServer.Collection
{
    using System.Collections.Generic;

    public class MulticastSet<T> : MulticastArray<T>
    {
        protected override ICollection<T> NewCollection()
        {
            return new HashSet<T>();
        }
    }
}
