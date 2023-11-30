namespace GVMServer.Ns.Collections
{
    public class Value<T> : IKey
        where T : new()
    {
        public T Values { get; set; }

        public Value()
        {
            this.Values = new T();
        }

        public Value(T value)
        {
            this.Values = value;
        }

        public string GetKey()
        {
            return this.Values.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                object self = this.Values;
                if (self == obj)
                {
                    return true;
                }
            }
            return this.Values.Equals(obj);
        }

        public override int GetHashCode()
        {
            return this.Values.GetHashCode();
        }

        public override string ToString()
        {
            return this.Values.ToString();
        }

        public static implicit operator T(Value<T> value)
        {
            return value.Values;
        }

        public static implicit operator Value<T>(T value)
        {
            return new Value<T>(value);
        }
    }
}
