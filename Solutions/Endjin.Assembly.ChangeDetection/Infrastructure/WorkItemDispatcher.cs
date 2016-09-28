using System;
using System.Collections.Generic;
using System.Threading;

namespace Endjin.Assembly.ChangeDetection.Infrastructure
{
    /// <summary>
    ///     Distribute units of work to n threads which read its data from
    ///     a blocking queue and process it is parallel.
    /// </summary>
    /// <typeparam name="T">Workitem type</typeparam>
    public class WorkItemDispatcher<T> : IDisposable
        where T : class
    {
        private const string DefaultName = "Workitem Dispatcher";

        private static readonly TypeHashes myType = new TypeHashes(typeof(WorkItemDispatcher<T>));

        private readonly WorkItemDispatcherData<T> myData;

        private readonly List<Exception> myExceptions = new List<Exception>();

        private readonly WaitHandle[] myExitEvents;

        private bool myCancel;

        private bool myIsDisposed;

        private int myRunningCount;

        /// <summary>
        ///     Create n-threads from the IO thread pool and process the data in the processor delegate which is fetched
        ///     from the work queue.
        /// </summary>
        /// <param name="width">Number of IO threads to start in parallel.</param>
        /// <param name="processor">Delegate which is called in width threads to process work.</param>
        /// <param name="work">Input queue which contains the work items for all threads.</param>
        public WorkItemDispatcher(int width, Action<T> processor, BlockingQueue<T> work) : this(new WorkItemDispatcherData<T>
        {
            Width = width,
            Processor = processor,
            InputData = work
        })
        {
        }

        /// <summary>
        ///     Create n-threads from the IO thread pool and process the data in the processor delegate which is fetched
        ///     from the work queue.
        /// </summary>
        /// <param name="width">Number of IO threads to start in parallel.</param>
        /// <param name="processor">Delegate which is called in width threads to process work.</param>
        /// <param name="work">Input queue which contains the work items for all threads.</param>
        /// <param name="options">Exception handling options.</param>
        public WorkItemDispatcher(int width, Action<T> processor, BlockingQueue<T> work, WorkItemOptions options) : this(new WorkItemDispatcherData<T>
        {
            Width = width,
            Processor = processor,
            InputData = work,
            Options = options
        })
        {
        }

        /// <summary>
        ///     Create n-threads from the IO thread pool and process the data in the processor delegate which is fetched
        ///     from the work queue.
        /// </summary>
        /// <param name="width">Number of IO threads to start in parallel.</param>
        /// <param name="processor">Delegate which is called in width threads to process work.</param>
        /// <param name="workList">List of blocking queues. The input queues are processed in the order they were added.</param>
        /// <param name="options">Exception handling options.</param>
        public WorkItemDispatcher(int width, Action<T> processor, BlockingQueueAggregator<T> workList, WorkItemOptions options) : this(width, processor, null, workList, options)
        {
        }

        /// <summary>
        ///     Create n-threads from the IO thread pool and process the data in the processor delegate which is fetched
        ///     from the work queue.
        /// </summary>
        /// <param name="width">Number of IO threads to start in parallel.</param>
        /// <param name="processor">Delegate which is called in width threads to process work.</param>
        /// <param name="name">Instance Name of the Dispatcher.</param>
        /// <param name="workList">List of blocking queues. The input queues are processed in the order they were added.</param>
        /// <param name="options">Exception handling options.</param>
        public WorkItemDispatcher(int width, Action<T> processor, string name, BlockingQueueAggregator<T> workList, WorkItemOptions options) : this(new WorkItemDispatcherData<T>
        {
            Width = width,
            Processor = processor,
            Name = name,
            InputDataList = workList,
            Options = options
        })
        {
        }

        /// <summary>
        ///     Create n-threads from the IO thread pool and process the data in the processor delegate which is fetched
        ///     from the work queue.
        /// </summary>
        /// <param name="width">Number of IO threads to start in parallel.</param>
        /// <param name="processor">Delegate which is called in width threads to process work.</param>
        /// <param name="name">Instance Name of the Dispatcher.</param>
        /// <param name="work">Input blocking queue from which work is fetched for the worker threads.</param>
        /// <param name="options">Exception handling options.</param>
        public WorkItemDispatcher(int width, Action<T> processor, string name, BlockingQueue<T> work, WorkItemOptions options) : this(new WorkItemDispatcherData<T>
        {
            Width = width,
            Processor = processor,
            Name = name,
            InputData = work,
            Options = options
        })
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="WorkItemDispatcher&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="width">Number of IO threads to start in parallel.</param>
        /// <param name="processor">Delegate which is called in width threads to process work.</param>
        /// <param name="name">Instance Name of the Dispatcher.</param>
        /// <param name="workList">List of blocking queues. The input queues are processed in the order they were added.</param>
        public WorkItemDispatcher(int width, Action<T> processor, string name, BlockingQueueAggregator<T> workList) : this(width, processor, name, workList, WorkItemOptions.Default)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="WorkItemDispatcher&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="width">Number of IO threads to start in parallel.</param>
        /// <param name="processor">Delegate which is called in width threads to process work.</param>
        /// <param name="name">Instance Name of the Dispatcher.</param>
        /// <param name="work">Input blocking queue from which work is fetched for the worker threads.</param>
        public WorkItemDispatcher(int width, Action<T> processor, string name, BlockingQueue<T> work) : this(width, processor, name, work, WorkItemOptions.Default)
        {
        }

        /// <summary>
        ///     Create n-threads from the IO thread pool and process the data in the processor delegate which is fetched
        ///     from the work queue.
        /// </summary>
        /// <param name="data">Class which configures the dispatcher.</param>
        public WorkItemDispatcher(WorkItemDispatcherData<T> data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (data.Processor == null)
            {
                throw new ArgumentNullException("data.Processor");
            }

            if (data.InputDataList == null)
            {
                throw new ArgumentNullException("data.InputDataList was null. No work given to dispatcher");
            }

            if (data.Width < 1)
            {
                throw new ArgumentOutOfRangeException("The Width (number of concurrent threads) must be > 0");
            }

            this.myData = data;
            this.SetName(data.Name);
            this.myData.Options = this.ConvertFromDefault(data.Options);
            this.myExitEvents = new WaitHandle[this.myData.Width];
            this.myRunningCount = this.myData.Width;

            Action processWork = this.ProcessWork;
            for (var i = 0; i < this.myData.Width; i++)
            {
                var res = processWork.BeginInvoke(this.Completed, null);
                this.myExitEvents[i] = res.AsyncWaitHandle;
            }
        }

        /// <summary>
        ///     Instance name of the dispatcher
        /// </summary>
        public string Name
        {
            get
            {
                return this.myData.Name;
            }
        }

        /// <summary>
        ///     Wait until all worker threads have processed all pending data and no more input data is available
        ///     in the input queues. An alternative is to use the OnCompleted callback in the
        ///     <see cref="T:WorkItemDispatcherData" /> class to get a notification
        /// </summary>
        public void Dispose()
        {
            using (var t = new Tracer(myType, "Dispose"))
            {
                if (!this.myIsDisposed)
                {
                    this.myIsDisposed = true;

                    t.Info("Wait until all workers have finished");
                    this.WaitUntilFinished();
                    t.Info("All worker have finished");

                    foreach (var wait in this.myExitEvents)
                    {
                        wait.Close();
                    }

                    // Throw excepton only if we are not already in exception processing
                    // to prevent masking the orginal excepton when the dispose is triggered
                    // via a using statement which is exited via an exception.
                    if (this.myExceptions.Count > 0 && !ExceptionHelper.InException)
                    {
                        this.ThrowWorkerException(false);
                    }
                }
            }
        }

        private WorkItemOptions ConvertFromDefault(WorkItemOptions option)
        {
            return option == WorkItemOptions.Default ? WorkItemOptions.ExitOnFirstEror : option;
        }

        private void SetName(string name)
        {
            this.myData.Name = string.IsNullOrEmpty(name) ? DefaultName : name;
        }

        private void Completed(IAsyncResult res)
        {
            using (var t = new Tracer(myType, "Completed"))
            {
                Interlocked.Decrement(ref this.myRunningCount);
                t.Info("Current running count: {0}", this.myRunningCount);

                if (this.myRunningCount == 0 && this.myData.OnCompleted != null)
                {
                    // Fire completion event when all threads are done
                    this.myData.OnCompleted(this.CreateException());
                }
            }
        }

        private AggregateException CreateException()
        {
            if (this.myExceptions.Count == 0)
            {
                return null;
            }

            return new AggregateException("One or more worker thread exceptions did occur.", this.myExceptions);
        }

        private void ThrowWorkerException(bool bThrowBackToEnqueuer)
        {
            lock (this.myExceptions)
            {
                if (this.myExceptions.Count > 0)
                {
                    if (this.IsEnabled(WorkItemOptions.ExitOnFirstEror) || !bThrowBackToEnqueuer)
                    {
                        throw this.CreateException();
                    }
                }
            }
        }

        private T GetNextWorkItem()
        {
            return this.myData.InputDataList.Dequeue();
        }

        private bool IsEnabled(WorkItemOptions option)
        {
            return ((this.myData.Options & option) == option);
        }

        /// <summary>
        ///     Worker thread which fetches work from the queue and processes it.
        /// </summary>
        private void ProcessWork()
        {
            using (var tr = new Tracer(myType, "ProcessWork"))
            {
                try
                {
                    while (true)
                    {
                        if (this.myCancel)
                        {
                            break;
                        }

                        var work = this.GetNextWorkItem();
                        if (work == null || (this.myExceptions.Count > 0 && this.IsEnabled(WorkItemOptions.ExitOnFirstEror)))
                        {
                            // No more work present or an error has happened
                            break;
                        }

                        try
                        {
                            // Do some work with work item.
                            this.myData.Processor(work);
                        }
                        catch (Exception ex)
                        {
                            tr.Error(Level.L2, ex, "Got exception in worker thread {0} while processing work item {1}", this.Name, work);
                            if (this.IsEnabled(WorkItemOptions.AggregateExceptions))
                            {
                                tr.Info("Aggregating Exception");
                                lock (this.myExceptions)
                                {
                                    this.myExceptions.Add(ex);
                                }
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    tr.Error(Level.L1, ex, "Worker thread {0} was interrupted by exception of worker thread", this.Name);
                    lock (this.myExceptions)
                    {
                        this.myExceptions.Add(ex);
                    }
                }
            }
        }

        /// <summary>
        ///     Cancel all pending threads. When all threads have finished processing their current work item
        ///     the dispose call will unblock. The OnCompleted delegate from the WorkitemDispatcherData class will
        ///     be called as usual.
        /// </summary>
        public void Cancel()
        {
            using (var t = new Tracer(myType, "Cancel"))
            {
                this.myCancel = true;
            }
        }

        /// <summary>
        ///     Cancel all pending threads and wait when they have finished processing the current work item.
        /// </summary>
        public void CancelAndWait()
        {
            using (var t = new Tracer(myType, "Cancel"))
            {
                this.Cancel();
                this.WaitUntilFinished();
            }
        }

        /// <summary>
        ///     Wait until all work has been processed by all worker threads.
        /// </summary>
        private void WaitUntilFinished()
        {
            // Windows does support wait only for a single WaitHandle. To work around this
            // we wait on another thread for our handles.
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                Action acc = () => WaitHandle.WaitAll(this.myExitEvents);
                acc.EndInvoke(acc.BeginInvoke(null, null));
            }
            else
            {
                WaitHandle.WaitAll(this.myExitEvents);
            }
        }
    }
}