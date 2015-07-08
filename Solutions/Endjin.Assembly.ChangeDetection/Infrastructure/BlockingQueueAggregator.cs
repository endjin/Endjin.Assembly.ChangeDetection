namespace AssemblyDifferences.Infrastructure
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     Aggregate one or a list of queues for processing with WorkItemDispatcher. The queues
    ///     are processed in the order of addition. Only when the first queue is empty the next
    ///     queue is used until no more queues are left.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BlockingQueueAggregator<T>
        where T : class
    {
        private readonly Queue<BlockingQueue<T>> myQueues = new Queue<BlockingQueue<T>>();

        private BlockingQueue<T> myCurrent;

        public BlockingQueueAggregator(BlockingQueue<T> queue)
        {
            if (queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            this.myQueues.Enqueue(queue);
        }

        public BlockingQueueAggregator(IEnumerable<BlockingQueue<T>> queues)
        {
            if (queues == null)
            {
                throw new ArgumentNullException("queues");
            }

            foreach (var queue in queues)
            {
                this.myQueues.Enqueue(queue);
            }
        }

        internal T Dequeue()
        {
            T lret = null;

            lock (this.myQueues)
            {
                TryNextQueue:
                if (this.myCurrent == null && this.myQueues.Count > 0)
                {
                    this.myCurrent = this.myQueues.Dequeue();
                }

                // No more queues available
                if (this.myCurrent == null)
                {
                    return lret;
                }

                // get next element or null if no more elements are in the queue
                lret = this.myCurrent.Dequeue();
                if (lret == null)
                {
                    this.myCurrent = null;
                    goto TryNextQueue;
                }
            }

            return lret;
        }
    }
}