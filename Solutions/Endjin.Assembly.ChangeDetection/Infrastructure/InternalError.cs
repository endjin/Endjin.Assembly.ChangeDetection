namespace AssemblyDifferences.Infrastructure
{
    using System;
    using System.Diagnostics;

    internal static class InternalError
    {
        private static readonly string Help = "Tracing can be enabled via the environment variable " + TracerConfig.TraceEnvVarName + Environment.NewLine + "Format: <Output Device>;<Type Filter> <Message Filter>; <Type Filter> <Message Filter>; ..." + Environment.NewLine + "    <Output Device>  Can be: Default, File, Debugoutput or Console." + Environment.NewLine + "                       Default             Write traces to configured trace listeners with the source name " + TracerConfig.TraceEnvVarName + " read from App.config" + Environment.NewLine + "                       File [FileName]     Write traces to given file name. The AppDomain name and process id are prepended to the file name to make it unique. If none given a trace file where the executable is located is created" + Environment.NewLine + "                       DebugOutput         Write to windows kernel via OutputDebugString which can be best viewed with dbgview from SysInternals" + Environment.NewLine + "                       Console             Write to stdout" + Environment.NewLine + "    <[!]TypeFilter>  It is basically the full qualified type name (case insensitive)." + Environment.NewLine + "                     If the TypeFilter begins with a ! character it is treated as exclusion filter." + Environment.NewLine + "                     Example: ApiChange.Infrastructure.AggregateException or * or ApiChange.* partial name matches like Api* are NOT supported" + Environment.NewLine + "                     " + Environment.NewLine + "    <Message Filter> Enable a specific trace level and/or severity filter" + Environment.NewLine + "                     Several filters can be combined with the + sign." + Environment.NewLine + "                     Allowed trace Levels are: Level*, Level1, Level2, ... Level5, LevelDispose. Shortcuts are l*, l1, ... l5, ldispose." + Environment.NewLine + "                     Severity Filters are:  All, InOut, Info, I, Information, Warning, Warn, W, Error, E, Exception, Ex" + Environment.NewLine + "                     Example: Level1+InOut+Info" + Environment.NewLine + "                     When no severity and or trace level is specified all levels/severities are enabled." + Environment.NewLine + " Enable full tracing (all severities and all levels) to debugoutput for all types except the ones which reside in the ApiChange.Infrastructure namespace" + Environment.NewLine + "    " + TracerConfig.TraceEnvVarName + "=debugoutput; ApiChange.* all;!ApiChange.Infrastructure.* all" + Environment.NewLine + " Enable file traces with Level1 for all types except the ones beneath the ApiChange.Infrastructure namespace" + Environment.NewLine + "    " + TracerConfig.TraceEnvVarName + "=file; * Level1;!ApiChange.Infrastructure.* all" + Environment.NewLine + " Trace all exceptions in the method where it is first encountered" + Environment.NewLine + "    " + TracerConfig.TraceEnvVarName + "=file c:\\temp\\exceptions.txt; * Exception";

        private static readonly DefaultTraceListener myOutDevice = new DefaultTraceListener();

        internal static void Print(string message)
        {
            myOutDevice.WriteLine(message);
            Console.WriteLine(message);
        }

        internal static void Print(string fmt, params object[] args)
        {
            Print(string.Format(fmt, args));
        }

        internal static void Print(Exception ex, string fmt, params object[] args)
        {
            Print(string.Format("{0}{1}{2}", string.Format(fmt, args), Environment.NewLine, ex));
        }

        internal static void PrintHelp()
        {
            Print(Help);
        }
    }
}