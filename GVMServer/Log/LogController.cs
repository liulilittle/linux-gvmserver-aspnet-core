namespace GVMServer.Log
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using GVMServer.DDD.Service;

    public class LogController : IServiceBase, IDisposable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Func<string> m_pLogDirectory = null;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private volatile bool m_bDispose = false;

        public const string DEBUG = "DEBUG";
        public const string ERROR = "ERROR";
        public const string WARNING = "WARNING";
        public const string INFO = "INFO";

        public LogController(Func<string> directory)
        {
            this.m_pLogDirectory = directory ?? throw new ArgumentNullException(nameof(directory));
            this.CheckGetFullPath(string.Empty, false);
        }

        ~LogController()
        {
            Dispose();
        }

        private string CheckGetFullPath(string category, bool no_throw)
        {
            string path = string.Empty;
            var logpath = this.m_pLogDirectory;
            if (logpath != null)
            {
                path = logpath();
            }

            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            path = Path.GetFullPath(path);
            if (!Directory.Exists(path))
            {
                if (no_throw)
                {
                    try
                    {
                        Directory.CreateDirectory(path);
                    }
                    catch (Exception)
                    {
                        return string.Empty;
                    }
                }
                else
                {
                    Directory.CreateDirectory(path);
                }
            }

            path = path + "/" + category;
            path = Path.GetFullPath(path);
            return path;
        }

        protected virtual string GetFullPath(string category)
        {
            if (string.IsNullOrEmpty(category))
            {
                return string.Empty;
            }

            return CheckGetFullPath(category, true);
        }

        public virtual bool Write(string category, string s)
        {
            if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(category))
            {
                return false;
            }

            string path = this.GetFullPath(category) + ".log";
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            FileStream fs = null;
            BinaryWriter bw = null;
            try
            {
                using (fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    try
                    {
                        fs.Seek(fs.Length, SeekOrigin.Begin);
                        using (bw = new BinaryWriter(fs))
                        {
                            try
                            {
                                bw.Write(Encoding.UTF8.GetBytes($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] "));
                                bw.Write(Encoding.UTF8.GetBytes(s));
                            }
                            catch (Exception)
                            {
                                return false;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public bool WriteLine(string category, string s)
        {
            string p = s + Environment.NewLine;
            return this.Write(category, p);
        }

        public bool Debug(string s)
        {
            return Write(DEBUG, s);
        }

        public bool Error(string s)
        {
            return Write(ERROR, s);
        }

        public bool Warning(string s)
        {
            return Write(WARNING, s);
        }

        public bool Info(string s)
        {
            return Write(INFO, s);
        }

        public bool DebugLine(string s)
        {
            return WriteLine(DEBUG, s);
        }

        public bool ErrorLine(string s)
        {
            return WriteLine(ERROR, s);
        }

        public bool WarningLine(string s)
        {
            return WriteLine(WARNING, s);
        }

        public bool InfoLine(string s)
        {
            return WriteLine(INFO, s);
        }

        public static LogController GetDefaultController()
        {
            return ServiceObjectContainer.Get<LogController>();
        }

        public static string CaptureStackTrace(Exception exception, int skip = 0)
        {
            if (exception is NullReferenceException)
            {
                Debugger.Break();
            }
            if (skip < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(skip));
            }
            string stackTrace = string.Empty;
            if (exception != null)
            {
                stackTrace = $"{exception.GetType().FullName}({exception.Message})[{exception.Source}]\r\nStackTrace\r\n{exception.StackTrace}\r\n\r\n"; // 堆栈回溯
            }
            Func<Assembly, string> GetModuleName = (assembly) =>
            {
                if (null == assembly)
                {
                    return string.Empty;
                }
                string assemblyName = assembly.FullName;
                if (string.IsNullOrEmpty(assemblyName))
                {
                    return string.Empty;
                }
                int ii = assemblyName.IndexOf(',');
                if (ii >= 0)
                {
                    assemblyName = assemblyName.Substring(0, ii);
                }
                return assemblyName;
            };
            stackTrace += "FullStack\r\n";
            foreach (StackFrame sf in new StackTrace(1 + skip).GetFrames())
            {
                MethodBase m = sf.GetMethod();
                if (m.DeclaringType == null)
                {
                    stackTrace += $"   at [{GetModuleName(m.Module.Assembly)}]global::{m.Name} in ";
                }
                else
                {
                    stackTrace += $"   at [{GetModuleName(m.Module.Assembly)}]{m.DeclaringType.FullName}.{m.Name} in ";
                }
                if (sf.HasSource())
                {
                    stackTrace += $"<{sf.GetFileName()}>({sf.GetFileLineNumber()}:{sf.GetFileColumnNumber()}) ";
                }
                stackTrace += $"E(IL_{sf.GetILOffset().ToString("x4")},{sf.GetNativeOffset()},{sf.GetNativeIP()},{sf.GetNativeImageBase()}) \r\n";
            }
            return stackTrace;
        }

        public virtual void Dispose()
        {
            lock (this)
            {
                if (!m_bDispose)
                {
                    m_bDispose = true;

                    m_pLogDirectory = null;
                }
            }

            GC.SuppressFinalize(this);
        }
    }
}
