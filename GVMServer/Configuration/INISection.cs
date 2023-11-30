namespace GVMServer.Configuration
{
    using System;
    using System.Collections.Generic;

    public class INISection : Dictionary<string, INIKey>, IEnumerable<string>, IEnumerable<INIKey>
    {
        public string Name
        {
            get;
            private set;
        }

        public INIDocument Document
        {
            get;
            private set;
        }

        public INISection(INIDocument document, string sectionName)
        {
            this.Document = document ?? throw new ArgumentNullException(nameof(document));
            this.Name = sectionName ?? throw new ArgumentNullException(nameof(sectionName));
            if (sectionName.Length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sectionName));
            }
        }

        public override string ToString()
        {
            return this.Name;
        }

        public virtual void Add(string key, string value)
        {
            this.Add(key, new INIKey(this, key, value));
        }

        IEnumerator<INIKey> IEnumerable<INIKey>.GetEnumerator()
        {
            return this.Values.GetEnumerator();
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return this.Keys.GetEnumerator();
        }
    }
}
