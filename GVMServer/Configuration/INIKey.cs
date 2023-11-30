namespace GVMServer.Configuration
{
    using System;

    public class INIKey
    {
        private readonly INISection m_section;

        public INISection GetSection()
        {
            return this.m_section;
        }

        public string Key { get; private set; }

        public string Value { get; set; }

        protected internal INIKey(INISection section, string key, string value)
        {
            this.m_section = section ?? throw new ArgumentNullException(nameof(section));
            this.Key = key ?? throw new ArgumentNullException(nameof(key));
            if (key.Length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(key));
            }
            this.Value = value;
        }

        public override string ToString()
        {
            return $"Key={this.Key}, Value={this.Value}";
        }
    }
}
