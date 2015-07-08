namespace AssemblyDifferences.Infrastructure
{
    using System;

    /// <summary>
    ///     Configures the WorkItemDispatcher.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WorkItemDispatcherData<T>
        where T : class
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="WorkItemDispatcherData&lt;T&gt;" /> class.
        /// </summary>
        public WorkItemDispatcherData()
        {
            this.Width = 1;
        }

        /// <summary>
        ///     Dispatcher Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Number of concurrent threads running which are fetching work from the InputDataList and processing it.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        ///     Delegate which is called when all work has been processed
        /// </summary>
        public Action<AggregateException> OnCompleted { get; set; }

        /// <summary>
        ///     Delegate which is called to process the actual work
        /// </summary>
        public Action<T> Processor { get; set; }

        /// <summary>
        ///     Error Handling options
        /// </summary>
        public WorkItemOptions Options { get; set; }

        /// <summary>
        ///     Adds a queue to the InputDataList. The queue can be full or be filled later when work is available.
        /// </summary>
        public BlockingQueue<T> InputData
        {
            set
            {
                this.InputDataList = new BlockingQueueAggregator<T>(value);
            }
        }

        /// <summary>
        ///     List of blocking queues which are processed by Width threads in via the Processor delegate in the order they were
        ///     entered.
        /// </summary>
        public BlockingQueueAggregator<T> InputDataList { set; get; }
    }
}