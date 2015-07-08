namespace AssemblyDifferences.Introspection
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using AssemblyDifferences.Infrastructure;

    internal class PdbDownLoader
    {
        private static readonly TypeHashes myType = new TypeHashes(typeof(PdbDownLoader));

        private readonly int myDownLoadThreadCount;

        private ISymChkExecutor myExecutor;

        public PdbDownLoader() : this(1)
        {
        }

        /// <summary>
        ///     Patch after the pdb was downloaded the drive letter to match the pdb files with the source files
        /// </summary>
        /// <param name="downLoadThreadCount">Down load thread count.</param>
        public PdbDownLoader(int downLoadThreadCount)
        {
            using (var t = new Tracer(myType, "PdbDownLoader"))
            {
                this.FailedPdbs = new List<string>();
                if (downLoadThreadCount <= 0)
                {
                    throw new ArgumentException("The download thread count cannot be <= 0");
                }

                this.myDownLoadThreadCount = downLoadThreadCount;
                t.Info("Download thread count is {0}", this.myDownLoadThreadCount);
            }
        }

        public int SucceededPdbCount { get; private set; }

        public List<string> FailedPdbs { get; set; }

        internal ISymChkExecutor Executor
        {
            get
            {
                lock (this)
                {
                    if (this.myExecutor == null)
                    {
                        this.myExecutor = new SymChkExecutor();
                    }
                    return this.myExecutor;
                }
            }

            set
            {
                this.myExecutor = value;
            }
        }

        private void DeleteOldPdb(string binaryName)
        {
            using (var t = new Tracer(Level.L5, myType, "DeleteOldPdb"))
            {
                var pdbFile = GetPdbNameFromBinaryName(binaryName);
                t.Info("Try to delete pdb {0} for binary {1}", pdbFile, binaryName);
                try
                {
                    File.Delete(pdbFile);
                }
                catch (FileNotFoundException ex)
                {
                    t.Error(ex, "No old pdb file did exist");
                }
                catch (Exception ex)
                {
                    t.Error(ex, "Could not delete old pdb file");
                }
            }
        }

        public bool DownloadPdbs(FileQuery query, string symbolServer)
        {
            return this.DownloadPdbs(query, symbolServer, null);
        }

        /// <summary>
        ///     Downloads the pdbs from the symbol server.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="symbolServer">The symbol server name.</param>
        /// <param name="downloadDir">The download directory. Can be null.</param>
        /// <returns>
        ///     true if all symbols could be downloaded. False otherwise.
        /// </returns>
        public bool DownloadPdbs(FileQuery query, string symbolServer, string downloadDir)
        {
            using (var t = new Tracer(myType, "DownloadPdbs"))
            {
                var lret = SymChkExecutor.bCanStartSymChk;
                var currentFailCount = this.FailedPdbs.Count;

                var fileQueue = query.EnumerateFiles;
                var aggregator = new BlockingQueueAggregator<string>(fileQueue);

                Action<string> downLoadPdbThread = (string fileName) =>
                {
                    var pdbFileName = GetPdbNameFromBinaryName(fileName);

                    // delete old pdb to ensure that the new matching pdb is really downloaded. Symchk does not replace existing but not matching pdbs.
                    try
                    {
                        File.Delete(pdbFileName);
                    }
                    catch
                    {
                    }

                    if (!this.Executor.DownLoadPdb(fileName, symbolServer, downloadDir))
                    {
                        lock (this.FailedPdbs)
                        {
                            this.FailedPdbs.Add(Path.GetFileName(fileName));
                        }
                    }
                    else
                    {
                        lock (this.FailedPdbs)
                        {
                            this.SucceededPdbCount++;
                        }
                    }
                };

                var dispatcher = new WorkItemDispatcher<string>(this.myDownLoadThreadCount, downLoadPdbThread, "Pdb Downloader", aggregator, WorkItemOptions.AggregateExceptions);

                try
                {
                    dispatcher.Dispose();
                }
                catch (AggregateException ex)
                {
                    t.Error(ex, "Got error during pdb download");
                    lret = false;
                }

                if (this.FailedPdbs.Count > currentFailCount)
                {
                    t.Warning("The failed pdb count has increased by {0}", this.FailedPdbs.Count - currentFailCount);
                    lret = false;
                }

                return lret;
            }
        }

        internal static string GetPdbNameFromBinaryName(string binaryFileName)
        {
            return Path.Combine(Path.GetDirectoryName(binaryFileName), Path.GetFileNameWithoutExtension(binaryFileName) + ".pdb");
        }
    }
}