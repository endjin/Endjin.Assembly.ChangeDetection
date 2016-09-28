using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Endjin.Assembly.ChangeDetection.Infrastructure
{
    internal class TraceCfgParser
    {
        private static readonly Dictionary<string, MessageTypes> myFlagTranslator = new Dictionary<string, MessageTypes>(StringComparer.OrdinalIgnoreCase)
        {
            { "inout", MessageTypes.InOut },
            { "info", MessageTypes.Info },
            { "i", MessageTypes.Info },
            { "information", MessageTypes.Info },
            { "instrument", MessageTypes.Instrument },
            { "warning", MessageTypes.Warning },
            { "warn", MessageTypes.Warning },
            { "w", MessageTypes.Warning },
            { "error", MessageTypes.Error },
            { "e", MessageTypes.Error },
            { "exception", MessageTypes.Exception },
            { "ex", MessageTypes.Exception },
            { "all", MessageTypes.All },
            { "*", MessageTypes.All }
        };

        private static readonly Dictionary<string, Level> myLevelTranslator = new Dictionary<string, Level>(StringComparer.OrdinalIgnoreCase)
        {
            { "l1", Level.L1 },
            { "l2", Level.L2 },
            { "l3", Level.L3 },
            { "l4", Level.L4 },
            { "l5", Level.L5 },
            { "ldispose", Level.Dispose },
            { "l*", Level.All },
            { "level1", Level.L1 },
            { "level2", Level.L2 },
            { "level3", Level.L3 },
            { "level4", Level.L4 },
            { "level5", Level.L5 },
            { "leveldispose", Level.Dispose },
            { "level*", Level.All }
        };

        private bool bHasError;

        public TraceFilter Filters;

        public TraceFilter NotFilters;

        public TraceListener OutDevice;

        public bool UseAppConfigListeners;

        /// <summary>
        ///     Format string is of the form
        ///     outDevice; type flag1+flag2+...;type flags; ...
        ///     where flags are a combination of trace markers
        /// </summary>
        /// <param name="config"></param>
        public TraceCfgParser(string config)
        {
            if (string.IsNullOrEmpty(config))
            {
                return;
            }

            var parts = config.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(str => str.Trim()).ToArray();

            foreach (var filter in this.GetFilters(parts, 1).Reverse())
            {
                var typeName = filter.Key.TrimStart('!');
                var bIsNotFilter = filter.Key.IndexOf('!') == 0;

                var levelAndMsgFilter = this.ParseMsgTypeFilter(filter.Value);

                var curFilterInstance = new TraceFilter(typeName, levelAndMsgFilter.Value, levelAndMsgFilter.Key, bIsNotFilter ? this.NotFilters : this.Filters);

                if (bIsNotFilter)
                {
                    this.NotFilters = curFilterInstance;
                }
                else
                {
                    this.Filters = curFilterInstance;
                }
            }

            if (parts.Length > 0)
            {
                this.OpenOutputDevice(parts[0].ToLower());
            }

            // when only output device was configured or wrong mask was entere we enable full tracing
            // by default
            if (this.Filters == null)
            {
                this.Filters = new TraceFilterMatchAll();
            }

            if (this.bHasError)
            {
                InternalError.PrintHelp();
            }
        }

        public static string DefaultTraceFileBaseName
        {
            get
            {
                var mainModule = Process.GetCurrentProcess().MainModule.FileName;

                return Path.Combine(Path.GetDirectoryName(mainModule), Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName) + ".txt");
            }
        }

        public static string DefaultExpandedTraceFileName
        {
            get
            {
                return AddPIDAndAppDomainNameToFileName(DefaultTraceFileBaseName);
            }
        }

        private IEnumerable<KeyValuePair<string, string[]>> GetFilters(string[] filters, int nSkip)
        {
            foreach (var current in filters.Skip(nSkip))
            {
                var filterParts = current.Split(new[] { '+', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (filterParts.Length < 2)
                {
                    this.bHasError = true;
                    InternalError.Print("The configuration string {0} did have an unmatched type severity or level filter part: {0}", current);
                }

                yield return new KeyValuePair<string, string[]>(filterParts[0], filterParts.Skip(1).ToArray());
            }
        }

        private void OpenOutputDevice(string outDevice)
        {
            var parts = outDevice.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var deviceName = parts[0];
            var deviceConfig = string.Join(" ", parts.Skip(1).ToArray());

            switch (deviceName)
            {
                case "file":
                    if (deviceConfig == "")
                    {
                        deviceConfig = DefaultTraceFileBaseName;
                    }
                    this.OutDevice = new TextWriterTraceListener(CreateTraceFile(deviceConfig));
                    break;

                case "debugoutput":
                    this.OutDevice = new DefaultTraceListener();
                    break;

                case "console":
                    this.OutDevice = new ConsoleTraceListener();
                    break;
                case "null":
                    this.OutDevice = new NullTraceListener();
                    break;

                case "default":
                    this.UseAppConfigListeners = true;
                    this.OutDevice = new NullTraceListener();
                    break;

                default:
                    InternalError.Print("The trace output device {0} is not supported.", outDevice);
                    this.bHasError = true;
                    break;
            }
        }

        private KeyValuePair<Level, MessageTypes> ParseMsgTypeFilter(string[] typeFilters)
        {
            var msgTypeFilter = MessageTypes.None;
            var level = Level.None;

            foreach (var filter in typeFilters)
            {
                var curFilter = MessageTypes.None;
                var curLevel = Level.None;

                if (!myFlagTranslator.TryGetValue(filter.Trim(), out curFilter))
                {
                    if (!myLevelTranslator.TryGetValue(filter.Trim(), out curLevel))
                    {
                        InternalError.Print("The trace message type filter string {0} was not expected.", filter);
                        this.bHasError = true;
                    }
                    else
                    {
                        level |= curLevel;
                    }
                }
                else
                {
                    msgTypeFilter |= curFilter;
                }
            }

            // if nothing was enabled we do enable full tracing by default
            msgTypeFilter = (msgTypeFilter == MessageTypes.None) ? MessageTypes.All : msgTypeFilter;
            level = (level == Level.None) ? Level.All : level;

            return new KeyValuePair<Level, MessageTypes>(level, msgTypeFilter);
        }

        public static TextWriter CreateTraceFile(string filebaseName)
        {
            var traceFileName = AddPIDAndAppDomainNameToFileName(Path.GetFullPath(filebaseName));
            var traceDir = Path.GetDirectoryName(traceFileName);

            FileStream fstream = null;
            var successFullyOpened = false;
            for (var i = 0; i < 2; i++) // Retry the open operation in case of errors
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(traceFileName)); // if the directory to the trace file does not exist create it
                    fstream = new FileStream(traceFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
                    successFullyOpened = true;
                }
                catch (IOException) // try to open the file with another name in case of a locking error
                {
                    traceDir = traceFileName + Guid.NewGuid();
                }

                if (successFullyOpened)
                {
                    break;
                }
            }

            if (fstream != null)
            {
                TextWriter writer = new StreamWriter(fstream);
                // Create a synchronized TextWriter to enforce proper locking in case of concurrent tracing to file
                writer = TextWriter.Synchronized(writer);
                return writer;
            }

            return null;
        }

        public static string AddPIDAndAppDomainNameToFileName(string file)
        {
            // any supplied PID would be useless since we always append the PID
            var fileName = Path.GetFileName(file).Replace("PID", "");

            var idx = fileName.LastIndexOf('.');
            if (idx == -1)
            {
                idx = fileName.Length;
            }

            var strippedAppDomainName = AppDomain.CurrentDomain.FriendlyName.Replace('.', '_');
            strippedAppDomainName = strippedAppDomainName.Replace(':', '_').Replace('\\', '_').Replace('/', '_');

            var pidAndAppDomainName = "_" + Process.GetCurrentProcess().Id + "_" + strippedAppDomainName;

            // insert process id and AppDomain name
            fileName = fileName.Insert(idx, pidAndAppDomainName);

            return Path.Combine(Path.GetDirectoryName(file), fileName);
        }
    }
}