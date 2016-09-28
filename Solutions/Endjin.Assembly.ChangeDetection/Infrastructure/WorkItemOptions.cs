using System;

namespace Endjin.Assembly.ChangeDetection.Infrastructure
{
    [Flags]
    public enum WorkItemOptions
    {
        /// <summary>
        ///     Use standard behaviour
        /// </summary>
        Default = 0,

        /// <summary>
        ///     When an exception occurs all worker threads are cancelled and the last worker thread exception is
        ///     rethrown.
        /// </summary>
        ExitOnFirstEror = 1,

        /// <summary>
        ///     Collect all exceptions from worker threads and throw an AggregateException when Dispose is called.
        /// </summary>
        AggregateExceptions = 2
    }
}