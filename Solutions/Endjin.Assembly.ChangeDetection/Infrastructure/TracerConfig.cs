namespace AssemblyDifferences.Infrastructure
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    /// <summary>
    ///     Main Class to configure trace output devices. The default instance is basically a null device.
    /// </summary>
    public class TracerConfig : IDisposable
    {
        /// <summary>
        ///     Environment variable which configures tracing.
        /// </summary>
        public const string TraceEnvVarName = "_Trace";

        internal static TracerConfig Instance = new TracerConfig(Environment.GetEnvironmentVariable(TraceEnvVarName));

        private static readonly object myLock = new object();

        private static readonly string myPid = Process.GetCurrentProcess().Id.ToString("D5");

        [ThreadStatic]
        private static string ProcessAndThreadId;

        private readonly TraceFilter myFilters = new TraceFilterMatchNone();

        private readonly TraceListenerCollection myListeners;

        private readonly TraceFilter myNotFilters;

        internal TracerConfig(string cfg)
        {
            if (string.IsNullOrEmpty(cfg))
            {
                return;
            }

            var source = new TraceSource(TraceEnvVarName, SourceLevels.All);
            this.myListeners = source.Listeners;

            var parser = new TraceCfgParser(cfg);
            var newListener = parser.OutDevice;
            this.myFilters = parser.Filters;
            this.myNotFilters = parser.NotFilters;

            if (newListener != null)
            {
                // when the App.config _Trace source should be used we do not replace
                // anything
                if (!parser.UseAppConfigListeners)
                {
                    this.myListeners.Clear();
                    this.myListeners.Add(newListener);
                }
            }
            else
            {
                this.myListeners = null;
            }
        }

        internal string PidAndTid
        {
            get
            {
                if (ProcessAndThreadId == null)
                {
                    ProcessAndThreadId = myPid + "/" + GetCurrentThreadId().ToString("D5");
                }

                return ProcessAndThreadId;
            }
        }

        internal static TraceListenerCollection Listeners
        {
            get
            {
                return Instance.myListeners;
            }
        }

        #region IDisposable Members

        /// <summary>
        ///     Close the current active trace listeners in a thread safe way.
        /// </summary>
        public void Dispose()
        {
            lock (myLock)
            {
                // The shutdown protocol works like this
                // 1. Get all listeners into an array
                // 2. Clear the thread safe listeners collection
                // 3. Call flush and dispose on each listener to ensure that any pending messages are written.
                // This way we ensure that while we are shutting the listerns down no additional trace messages
                // arrive which could be used accidentally by tracing. Using a disposed listener is almost always a bad idea.
                if (this.myListeners != null && this.myListeners.Count > 0)
                {
                    var listeners = new TraceListener[this.myListeners.Count];
                    this.myListeners.CopyTo(listeners, 0);
                    this.myListeners.Clear();
                    foreach (var listener in listeners)
                    {
                        listener.Flush();
                        listener.Dispose();
                    }
                }
            }
        }

        #endregion

        /// <summary>
        ///     Re/Set trace configuration in a thread safe way by shutting down the already existing listeners and then
        ///     put the new config into place.
        /// </summary>
        /// <param name="cfg">
        ///     The trace string format is of the form OutputDevice;TypeFilter MessageFilter; TypeFilter
        ///     MessageFilter; ...
        /// </param>
        /// <param name="bClearEvents">if true all registered trace callbacks are removed.</param>
        /// <returns>The old trace configuration string.</returns>
        public static string Reset(string cfg, bool bClearEvents)
        {
            lock (myLock)
            {
                Instance.Dispose();
                var old = Environment.GetEnvironmentVariable(TraceEnvVarName);
                Environment.SetEnvironmentVariable(TraceEnvVarName, cfg);
                Instance = new TracerConfig(Environment.GetEnvironmentVariable(TraceEnvVarName));
                if (bClearEvents)
                {
                    Tracer.ClearEvents();
                }
                return old;
            }
        }

        /// <summary>
        ///     Re/Set trace configuration in a thread safe way by shutting down the already existing listeners and then
        ///     put the new config into place.
        /// </summary>
        /// <param name="cfg">
        ///     The trace string format is of the form OutputDevice;TypeFilter MessageFilter; TypeFilter
        ///     MessageFilter; ...
        /// </param>
        /// <returns>The old trace configuration string.</returns>
        public static string Reset(string cfg)
        {
            return Reset(cfg, true);
        }

        internal bool IsEnabled(TypeHashes type, MessageTypes msgType, Level level)
        {
            if (this.myListeners == null || type == null)
            {
                return false;
            }

            var lret = this.myFilters.IsMatch(type, msgType, level);
            if (this.myNotFilters != null && lret)
            {
                lret = this.myNotFilters.IsMatch(type, msgType, level);
            }

            return lret;
        }

        internal void WriteTraceMessage(string traceMsg)
        {
            foreach (TraceListener listener in Listeners)
            {
                listener.Write(traceMsg);
                listener.Flush();
            }
        }

        /// <summary>
        ///     Get the current unmanaged Thread ID.
        /// </summary>
        /// <returns>Integer that identifies the current thread.</returns>
        [DllImport("kernel32.dll")]
        public static extern int GetCurrentThreadId();
    }
}