namespace AssemblyDifferences.Infrastructure
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    ///     A blocking queue which supports end markers to signal that no more work is left by inserting
    ///     a null reference. This constrains the queue to reference types only.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BlockingQueue<T> : IEnumerable<T>, IEnumerable, IDisposable
        where T : class
    {
        private readonly ManualResetEvent myEmptyEvent = new ManualResetEvent(false);

        /// <summary>
        ///     The queue used to store the elements
        /// </summary>
        private readonly Queue<T> myQueue = new Queue<T>();

        private bool myAllItemsProcessed;

        #region IDisposable Members

        /// <summary>
        ///     Closes the EmptyEvent WaitHandle.
        /// </summary>
        public void Dispose()
        {
            this.myEmptyEvent.Close();
        }

        #endregion

        /// <summary>
        ///     Returns an IEnumerator of Type T for this queue
        /// </summary>
        /// <returns></returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            while (true)
            {
                var item = this.Dequeue();
                if (item == null)
                {
                    break;
                }
                yield return item;
            }
        }

        /// <summary>
        ///     Returns a untyped IEnumerator for this queue
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        /// <summary>
        ///     Deques an element from the queue and returns it.
        ///     If the queue is empty the thread will block. If the queue is stopped it will immedieately
        ///     return with null.
        /// </summary>
        /// <returns>An object of type T</returns>
        public T Dequeue()
        {
            if (this.myAllItemsProcessed)
            {
                return null;
            }

            lock (this.myQueue)
            {
                while (this.myQueue.Count == 0)
                {
                    if (!Monitor.Wait(this.myQueue, 45))
                    {
                        // dispatch any work which is not done yet
                        if (this.myQueue.Count > 0)
                        {
                            continue;
                        }
                    }

                    // finito 
                    if (this.myAllItemsProcessed)
                    {
                        return null;
                    }
                }

                var result = this.myQueue.Dequeue();
                if (result == null)
                {
                    this.myAllItemsProcessed = true;
                    this.myEmptyEvent.Set();
                }
                return result;
            }
        }

        /// <summary>
        ///     Releases the waiters by enqueuing a null reference which causes all waiters to be released.
        ///     The will then get a null reference as queued element to signal that they should terminate.
        /// </summary>
        public void ReleaseWaiters()
        {
            this.Enqueue(null);
        }

        /// <summary>
        ///     Waits the until empty. This does not mean that all items are already process. Only that
        ///     the queue contains no more pending work.
        /// </summary>
        public void WaitUntilEmpty()
        {
            this.myEmptyEvent.WaitOne();
        }

        /// <summary>
        ///     Adds an element of type T to the queue.
        ///     The consumer thread is notified (if waiting)
        /// </summary>
        /// <param name="data_in">An object of type T</param>
        public void Enqueue(T data_in)
        {
            lock (this.myQueue)
            {
                this.myQueue.Enqueue(data_in);
                Monitor.PulseAll(this.myQueue);
            }
        }
    }
}