namespace GVMServer.Log
{
    using GVMServer.DDD.Service;
    using GVMServer.Threading;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    public class StatisticsController : IServiceBase, IDisposable
    {
        private ConcurrentDictionary<string, StopWatch> m_pStopWatchTable = new ConcurrentDictionary<string, StopWatch>();
        private ConcurrentDictionary<string, Counter> m_pCounterTable = new ConcurrentDictionary<string, Counter>();
        private Timer m_pMaintenanceTimer = new Timer();
        private Timer m_pDumpStatusTimer = new Timer();
        private volatile bool m_bDisposed = false;

        private class Counter
        {
            public long total_counter;
            public long max_counter;
            public long min_counter;
            public long current_counter;
        }

        private class StopWatch
        {
            public long total_ticks;
            public long max_ticks;
            public long min_ticks;
            public long current_ticks;
            public long last_ticks;
            public long begin_ticks;
        }

        public StatisticsController()
        {
            this.m_pMaintenanceTimer.Tick += (sender, e) => this.DoMaintenance();
            this.m_pMaintenanceTimer.Interval = 1000;
            this.m_pMaintenanceTimer.Start();

            this.m_pDumpStatusTimer.Tick += (sender, e) => this.DoDumpStatus();
            this.m_pDumpStatusTimer.Interval = 60000;
            this.m_pDumpStatusTimer.Start();
        }

        ~StatisticsController()
        {
            Dispose();
        }

        protected virtual void DoMaintenance()
        {
            foreach (Counter counter in m_pCounterTable.Values)
            {
                if (counter.current_counter > 0)
                {
                    if (0 >= counter.max_counter || counter.current_counter > counter.max_counter)
                    {
                        counter.max_counter = counter.current_counter;
                    }

                    if (0 >= counter.min_counter || counter.min_counter > counter.current_counter)
                    {
                        counter.min_counter = counter.current_counter;
                    }
                }
                counter.current_counter = 0;
            }

            foreach (StopWatch stopwatch in m_pStopWatchTable.Values)
            {
                if (stopwatch.current_ticks > 0)
                {
                    if (0 >= stopwatch.max_ticks || stopwatch.current_ticks > stopwatch.max_ticks)
                    {
                        stopwatch.max_ticks = stopwatch.current_ticks;
                    }

                    if (0 >= stopwatch.min_ticks || stopwatch.min_ticks > stopwatch.current_ticks)
                    {
                        stopwatch.min_ticks = stopwatch.current_ticks;
                    }
                }
                stopwatch.current_ticks = 0;
            }
        }

        protected virtual void DoDumpStatus()
        {
            LogController log = LogController.GetDefaultController();
            if (log != null)
            {
                string s = DumpStatus();
                s = string.Format("{0}{1}", Environment.NewLine, s);
                log.Debug(s);
            }
        }

        public static int MeasureWidth<TValue>(IDictionary<string, TValue> d)
        {
            if (d == null)
            {
                return 0;
            }

            int width = 0;
            foreach (string s in d.Keys)
            {
                if (string.IsNullOrEmpty(s))
                {
                    continue;
                }

                if (0 == width || s.Length > width)
                {
                    width = s.Length;
                }
            }

            return width;
        }

        private static T Max<T>(T x, T y) where T : IComparable<T>
        {
            int hr = x.CompareTo(y);
            return hr > 0 ? x : y;
        }

        public virtual string DumpStatus()
        {
            string content = string.Empty;
            int width = Max(MeasureWidth(m_pCounterTable), MeasureWidth(m_pStopWatchTable)) + 2;
            if (width < 9)
            {
                width = 9 + 2;
            }

            content += "Counter".PadRight(width);
            content += "Min".PadRight(21);
            content += "Max".PadRight(21);
            content += "Now".PadRight(21);
            content += "All".PadRight(21);
            content += "\r\n";

            content += "+".PadRight(width, '-');
            content += "+".PadRight(21, '-');
            content += "+".PadRight(21, '-');
            content += "+".PadRight(21, '-');
            content += "+".PadRight(21, '-');
            content += "".PadRight(149 - width - 21 - 21 - 21 - 21, '-');
            content += "+\r\n";

            foreach (KeyValuePair<string, Counter> pair in m_pCounterTable)
            {
                content += (pair.Key ?? string.Empty).PadRight(width);

                Counter counter = pair.Value;
                content += counter.min_counter.ToString().PadRight(21);
                content += counter.max_counter.ToString().PadRight(21);
                content += counter.current_counter.ToString().PadRight(21);
                content += counter.total_counter.ToString().PadRight(21);
                content += "\r\n";
            }

            content += "\r\n";
            content += "StopWatch".PadRight(width);
            content += "Min".PadRight(21);
            content += "Max".PadRight(21);
            content += "All".PadRight(21);
            content += "Now/Last".PadRight(21);
            content += "\r\n";

            content += "+".PadRight(width, '-');
            content += "+".PadRight(21, '-');
            content += "+".PadRight(21, '-');
            content += "+".PadRight(21, '-');
            content += "+".PadRight(21, '-');
            content += "".PadRight(149 - width - 21 - 21 - 21 - 21, '-');
            content += "+\r\n";

            foreach (KeyValuePair<string, StopWatch> pair in m_pStopWatchTable)
            {
                content += (pair.Key ?? string.Empty).PadRight(width);

                StopWatch stopWatch = pair.Value;
                content += stopWatch.min_ticks.ToString().PadRight(21);
                content += stopWatch.max_ticks.ToString().PadRight(21);
                content += stopWatch.total_ticks.ToString().PadRight(21);
                content += (stopWatch.current_ticks + "/" + stopWatch.last_ticks).PadRight(21);
                content += "\r\n";
            }

            return content;
        }

        public long AddCounter(string category, long value = 1)
        {
            if (string.IsNullOrEmpty(category))
            {
                return ~0;
            }

            if (!m_pCounterTable.ContainsKey(category))
            {
                if (value < 0)
                {
                    return ~0;
                }

                Counter counter = new Counter() { total_counter = value, current_counter = value };
                m_pCounterTable.TryAdd(category, counter);
                return counter.total_counter;
            }
            else
            {
                Counter counter = m_pCounterTable[category];
                do
                {
                    counter.total_counter += value;
                    counter.current_counter += value;
                } while (false);
                return counter.total_counter;
            }
        }

        public long SubCounter(string category, long value = -1)
        {
            return AddCounter(category, -value);
        }

        public long StartStopWatch(string category)
        {
            if (string.IsNullOrEmpty(category))
            {
                return ~0;
            }

            StopWatch stopWatch = null;
            if (!m_pStopWatchTable.ContainsKey(category))
            {
                stopWatch = new StopWatch();
                m_pStopWatchTable.TryAdd(category, stopWatch);
            }
            else
            {
                stopWatch = m_pStopWatchTable[category];
                if (stopWatch == null)
                {
                    return ~0;
                }

                if (stopWatch.begin_ticks > 0)
                {
                    return ~0;
                }
            }

            stopWatch.begin_ticks = DateTime.Now.Ticks;
            return stopWatch.begin_ticks;
        }

        public long StopStopWatch(string category)
        {
            if (string.IsNullOrEmpty(category))
            {
                return ~0;
            }

            StopWatch stopWatch = null;
            m_pStopWatchTable.TryGetValue(category, out stopWatch);

            if (stopWatch == null)
            {
                return ~0;
            }

            if (0 >= stopWatch.begin_ticks)
            {
                return ~0;
            }

            long current_ticks = DateTime.Now.Ticks;
            long diff_ticks = current_ticks - stopWatch.begin_ticks;

            stopWatch.total_ticks += diff_ticks;
            stopWatch.current_ticks += diff_ticks;
            stopWatch.last_ticks = current_ticks;

            stopWatch.begin_ticks = 0;

            return current_ticks;
        }

        public static StatisticsController GetDefaultController()
        {
            StatisticsController controller = ServiceObjectContainer.Get<StatisticsController>();
            return controller;
        }

        public virtual void Dispose()
        {
            lock (this)
            {
                if (!m_bDisposed)
                {
                    m_bDisposed = true;

                    m_pStopWatchTable?.Clear();
                    m_pCounterTable?.Clear();
                    m_pMaintenanceTimer?.Close();
                    m_pDumpStatusTimer?.Close();

                    m_pStopWatchTable = null;
                    m_pCounterTable = null;
                    m_pDumpStatusTimer = null;
                    m_pMaintenanceTimer = null;
                }
            }

            GC.SuppressFinalize(this);
        }
    }
}
