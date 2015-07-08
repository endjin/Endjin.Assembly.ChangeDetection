namespace AssemblyDifferences.Infrastructure
{
    using System.Diagnostics;

    internal class NullTraceListener : TraceListener
    {
        public override void Write(string message)
        {
        }

        public override void WriteLine(object o)
        {
        }

        public override void WriteLine(string message)
        {
        }
    }
}