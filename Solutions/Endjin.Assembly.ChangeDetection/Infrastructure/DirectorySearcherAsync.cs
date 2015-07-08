namespace AssemblyDifferences.Infrastructure
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;

    public class DirectorySearcherAsync : IEnumerable<string>
    {
        private static readonly TypeHashes myType = new TypeHashes(typeof(DirectorySearcherAsync));

        private readonly ManualResetEvent myHasFileEvent = new ManualResetEvent(false);

        private readonly ManualResetEvent myHasFinishedEvent = new ManualResetEvent(false);

        private readonly SearchOption mySearchOption;

        private readonly string mySearchPath;

        private readonly string mySearchPattern;

        internal List<string> myFiles = new List<string>();

        internal List<BlockingQueue<string>> myListeningQueues = new List<BlockingQueue<string>>();

        private volatile SearchState mySearchState;

        public DirectorySearcherAsync(string searchPath) : this(searchPath, "*", SearchOption.TopDirectoryOnly)
        {
        }

        public DirectorySearcherAsync(string searchPath, string searchPattern) : this(searchPath, searchPattern, SearchOption.TopDirectoryOnly)
        {
        }

        public DirectorySearcherAsync(string searchPath, string searchPattern, SearchOption searchOption)
        {
            if (string.IsNullOrEmpty(searchPath))
            {
                throw new ArgumentNullException("searchPath");
            }

            if (!Directory.Exists(searchPath))
            {
                throw new DirectoryNotFoundException(string.Format("The search path does not exist: {0}", searchPath));
            }

            if (string.IsNullOrEmpty(searchPattern))
            {
                throw new ArgumentNullException("searchPattern");
            }

            this.mySearchPath = Path.GetFullPath(searchPath);
            Tracer.Info(Level.L1, myType, "DirectorySearcherAsync", "Search in path {0} for {1}", this.mySearchPath, searchPattern);

            this.mySearchPattern = searchPattern;
            this.mySearchOption = searchOption;
        }

        public bool HasMatchingFiles
        {
            get
            {
                this.BeginSearch();
                var idx = WaitHandle.WaitAny(new WaitHandle[] { this.myHasFileEvent, this.myHasFinishedEvent });
                return idx == 0;
            }
        }

        public IEnumerator<string> GetEnumerator()
        {
            this.BeginSearch();
            return new DirectorySearcherAsyncEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            this.BeginSearch();
            return new DirectorySearcherAsyncEnumerator(this);
        }

        private void FindFileThread()
        {
            using (var t = new Tracer(Level.L3, myType, "FindFileThread"))
            {
                try
                {
                    try
                    {
                        var bHasSetFirstFileEvent = false;
                        var fileenum = new FileEnumerator(this.mySearchPath, this.mySearchPattern, this.mySearchOption);
                        while (fileenum.MoveNext())
                        {
                            lock (this)
                            {
                                this.myFiles.Add(fileenum.Current);
                                t.Info("Found file {0}, Listening Queue Count: {1}", fileenum.Current, this.myListeningQueues.Count);

                                foreach (var listeningQueue in this.myListeningQueues)
                                {
                                    listeningQueue.Enqueue(fileenum.Current);
                                }

                                if (!bHasSetFirstFileEvent)
                                {
                                    this.myHasFileEvent.Set();
                                    bHasSetFirstFileEvent = true;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        t.Error(ex, "Got error while searching in {0} for pattern {1}.", this.mySearchPath, this.mySearchPattern);
                    }
                }
                finally
                {
                    lock (this)
                    {
                        this.mySearchState = SearchState.Finished;
                        t.Info("Finished searching, Listening Queue count: {0}", this.myListeningQueues.Count);
                        this.myHasFinishedEvent.Set();

                        foreach (var listeningQueue in this.myListeningQueues)
                        {
                            listeningQueue.ReleaseWaiters();
                        }
                        this.myListeningQueues.Clear();
                    }
                }
            }
        }

        public void BeginSearch()
        {
            lock (this)
            {
                if (this.mySearchState != SearchState.NotStartedYet)
                {
                    return;
                }

                this.mySearchState = SearchState.Running;
            }

            Tracer.Info(Level.L3, myType, "BeginSearch", "Called Searcher.BeginInvoke");
            Action searcher = this.FindFileThread;
            searcher.BeginInvoke(null, null);
        }

        public BlockingQueue<string> GetResultQueue()
        {
            using (var t = new Tracer(Level.L3, myType, "GetResultQueue"))
            {
                this.BeginSearch();
                var queue = new BlockingQueue<string>();
                lock (this)
                {
                    foreach (var foundFile in this.myFiles)
                    {
                        queue.Enqueue(foundFile);
                    }

                    t.Info("Queue did get {0} files we found so far. Current State: {1}, Path: {2}", this.myFiles.Count, this.mySearchState, this.mySearchPath);

                    // If we still expect files register our queue to get notified
                    // when something new has arrived.
                    if (!this.myHasFinishedEvent.WaitOne(0))
                    {
                        t.Info("Search is still running. Add it as listener for other found files.");
                        this.myListeningQueues.Add(queue);
                    }
                    else
                    {
                        // Add final item to queue to signal that no more items will 
                        // be received.
                        queue.ReleaseWaiters();
                        t.Info("Closed queue because no more files are expected");
                    }
                }

                return queue;
            }
        }

        private enum SearchState
        {
            NotStartedYet,

            Running,

            Finished
        }

        private class DirectorySearcherAsyncEnumerator : IEnumerator<string>, IDisposable
        {
            private BlockingQueue<string> myResultQueue;

            private DirectorySearcherAsync mySearcher;

            public DirectorySearcherAsyncEnumerator(DirectorySearcherAsync searcher)
            {
                if (searcher == null)
                {
                    throw new ArgumentNullException("searcher");
                }

                this.mySearcher = searcher;

                this.mySearcher.BeginSearch();
                this.myResultQueue = this.mySearcher.GetResultQueue();
            }

            #region IDisposable Members

            public void Dispose()
            {
                this.mySearcher = null;
                this.Current = null;
                this.myResultQueue = null;
            }

            #endregion

            #region IEnumerator<string> Members

            public string Current { get; private set; }

            object IEnumerator.Current
            {
                get
                {
                    return this.Current;
                }
            }

            public bool MoveNext()
            {
                var lret = true;
                this.Current = this.myResultQueue.Dequeue();
                if (this.Current == null)
                {
                    lret = false;
                }
                return lret;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            #endregion
        }
    }
}