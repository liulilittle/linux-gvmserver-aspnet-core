namespace GVMServer.Configuration
{
    using GVMServer.IO;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;

    public class INIDocument : Dictionary<string, INISection>, IEnumerable<string>, IEnumerable<INISection>
    {
        private static readonly Encoding defaultEncoding = Encoding.UTF8;

        public void Load(string path)
        {
            this.Load(path, defaultEncoding);
        }

        protected Encoding GetEncoding(string path, Encoding encoding)
        {
            encoding = FileAuxiliary.GetEncoding(path, encoding ?? defaultEncoding);
            if (encoding == null)
            {
                encoding = defaultEncoding;
            }
            return encoding;
        }

        public virtual void Load(string path, Encoding encoding)
        {
            this.Clear();
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                encoding = GetEncoding(path, encoding);
                using (StreamReader sr = new StreamReader(fs, encoding))
                {
                    INISection section = null;
                    while (!sr.EndOfStream)
                    {
                        string s = sr.ReadLine();
                        if (string.IsNullOrEmpty(s))
                        {
                            continue;
                        }
                        s = s.TrimStart();
                        if (string.IsNullOrEmpty(s))
                        {
                            continue;
                        }
                        if (s[0] == '#')
                        {
                            continue;
                        }
                        if (s[0] == '[')
                        {
                            int i = s.IndexOf(']', 1);
                            if (i < 0)
                            {
                                continue;
                            }
                            section = this.NewSection(s.Substring(1, i - 1));
                            this.TryAdd(section.Name, section);
                        }
                        else if (section != null)
                        {
                            int i = s.IndexOf('=');
                            if (i <= 0)
                            {
                                continue;
                            }
                            string key = s.Substring(0, i);
                            string value = s.Substring(i + 1);
                            section.TryAdd(key, this.NewKey(section, key, value));
                        }
                    }
                }
            }
        }

        public void SaveAs(string path)
        {
            this.SaveAs(path, defaultEncoding);
        }

        public virtual void SaveAs(string path, Encoding encoding)
        {
            if (File.Exists(path))
            {
                encoding = GetEncoding(path, encoding);
                File.Delete(path);
            }
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
            {
                using (StreamWriter sw = new StreamWriter(fs, encoding))
                {
                    IEnumerable<INISection> sections = this;
                    foreach (INISection section in sections)
                    {
                        IEnumerable<INIKey> keys = section;
                        sw.WriteLine($"[{section.Name}]");
                        foreach (INIKey key in keys)
                        {
                            sw.WriteLine($"{key.Key}={key.Value}");
                        }
                        sw.WriteLine(string.Empty);
                    }
                }
            }
        }

        IEnumerator<INISection> IEnumerable<INISection>.GetEnumerator()
        {
            return this.Values.GetEnumerator();
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return this.Keys.GetEnumerator();
        }

        protected virtual INISection NewSection(string sectionName)
        {
            return new INISection(this, sectionName);
        }

        protected virtual INIKey NewKey(INISection section, string key, string value)
        {
            return new INIKey(section, key, value);
        }
    }
}
