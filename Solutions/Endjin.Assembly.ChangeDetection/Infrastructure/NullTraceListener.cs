using System.Diagnostics;

namespace Endjin.Assembly.ChangeDetection.Infrastructure
{
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