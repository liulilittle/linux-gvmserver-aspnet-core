namespace GVMServer.Planning.PlanningXml
{
    using System;
    using System.Collections.Generic;

    interface IConfigurationLoader
    {
        IList<T> GetAll<T>() where T : class;
    }

    public class KeyFileStruct
    {
        public KeyFileStruct(string key, string fileName)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentOutOfRangeException(nameof(key));
            }
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentOutOfRangeException(nameof(fileName));
            }
            this.Key = key;
            this.FileName = fileName;
        }

        public string Key { set; get; }

        public string FileName { set; get; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class XmlFileNameAttribute : Attribute
    {
        public KeyFileStruct Key { get; }

        public XmlFileNameAttribute(string key, string fileName)
        {
            this.Key = new KeyFileStruct(key, fileName);
        }
    }
}
