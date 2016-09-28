using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace Endjin.Assembly.ChangeDetection.Infrastructure
{
    /// <summary>
    ///     Provides the implementation of the
    ///     <see cref="T:System.Collections.Generic.IEnumerator`1" /> interface
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    internal class FileEnumerator : IEnumerator<string>
    {
        private readonly Stack<SearchContext> m_contextStack;

        private readonly string m_filter;

        private readonly SearchOption m_searchOption;

        private readonly WIN32_FIND_DATA m_win_find_data = new WIN32_FIND_DATA();

        private SearchContext m_currentContext;

        private SafeFindHandle m_hndFindFile;

        private string m_path;

        /// <summary>
        ///     Initializes a new instance of the <see cref="FileEnumerator" /> class.
        /// </summary>
        /// <param name="path">The path to search.</param>
        /// <param name="filter">The search string to match against files in the path.</param>
        /// <param name="searchOption">
        ///     One of the SearchOption values that specifies whether the search
        ///     operation should include all subdirectories or only the current directory.
        /// </param>
        public FileEnumerator(string path, string filter, SearchOption searchOption)
        {
            this.m_path = path;
            this.m_filter = filter;
            this.m_searchOption = searchOption;
            this.m_currentContext = new SearchContext(path);

            if (this.m_searchOption == SearchOption.AllDirectories)
            {
                this.m_contextStack = new Stack<SearchContext>();
            }
        }

        #region IEnumerator<FileData> Members

        /// <summary>
        ///     Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <value></value>
        /// <returns>
        ///     The element in the collection at the current position of the enumerator.
        /// </returns>
        public string Current
        {
            get
            {
                return Path.Combine(this.m_path, this.m_win_find_data.cFileName);
            }
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing,
        ///     or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (this.m_hndFindFile != null)
            {
                this.m_hndFindFile.Dispose();
            }
        }

        #endregion

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeFindHandle FindFirstFile(string fileName, [In] [Out] WIN32_FIND_DATA data);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool FindNextFile(SafeFindHandle hndFindFile, [In] [Out] [MarshalAs(UnmanagedType.LPStruct)] WIN32_FIND_DATA lpFindFileData);

        /// <summary>
        ///     Hold context information about where we current are in the directory search.
        /// </summary>
        private class SearchContext
        {
            public readonly string Path;

            public Stack<string> SubdirectoriesToProcess;

            public SearchContext(string path)
            {
                this.Path = path;
            }
        }

        #region IEnumerator Members

        /// <summary>
        ///     Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <value></value>
        /// <returns>
        ///     The element in the collection at the current position of the enumerator.
        /// </returns>
        object IEnumerator.Current
        {
            get
            {
                return this.m_path;
            }
        }

        /// <summary>
        ///     Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        ///     true if the enumerator was successfully advanced to the next element;
        ///     false if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">
        ///     The collection was modified after the enumerator was created.
        /// </exception>
        public bool MoveNext()
        {
            var retval = false;

            //If the handle is null, this is first call to MoveNext in the current 
            // directory.  In that case, start a new search.
            if (this.m_currentContext.SubdirectoriesToProcess == null)
            {
                if (this.m_hndFindFile == null)
                {
                    new FileIOPermission(FileIOPermissionAccess.PathDiscovery, this.m_path).Demand();

                    var searchPath = Path.Combine(this.m_path, this.m_filter);
                    this.m_hndFindFile = FindFirstFile(searchPath, this.m_win_find_data);
                    retval = !this.m_hndFindFile.IsInvalid;
                }
                else
                {
                    //Otherwise, find the next item.
                    retval = FindNextFile(this.m_hndFindFile, this.m_win_find_data);
                }
            }

            //If the call to FindNextFile or FindFirstFile succeeded...
            if (retval)
            {
                if ((this.m_win_find_data.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    //Ignore folders for now.   We call MoveNext recursively here to 
                    // move to the next item that FindNextFile will return.
                    return this.MoveNext();
                }
            }
            else if (this.m_searchOption == SearchOption.AllDirectories)
            {
                //SearchContext context = new SearchContext(m_hndFindFile, m_path);
                //m_contextStack.Push(context);
                //m_path = Path.Combine(m_path, m_win_find_data.cFileName);
                //m_hndFindFile = null;

                if (this.m_currentContext.SubdirectoriesToProcess == null)
                {
                    var subDirectories = Directory.GetDirectories(this.m_path);
                    this.m_currentContext.SubdirectoriesToProcess = new Stack<string>(subDirectories);
                }

                if (this.m_currentContext.SubdirectoriesToProcess.Count > 0)
                {
                    var subDir = this.m_currentContext.SubdirectoriesToProcess.Pop();

                    this.m_contextStack.Push(this.m_currentContext);
                    this.m_path = subDir;
                    this.m_hndFindFile = null;
                    this.m_currentContext = new SearchContext(this.m_path);
                    return this.MoveNext();
                }

                //If there are no more files in this directory and we are 
                // in a sub directory, pop back up to the parent directory and
                // continue the search from there.
                if (this.m_contextStack.Count > 0)
                {
                    this.m_currentContext = this.m_contextStack.Pop();
                    this.m_path = this.m_currentContext.Path;
                    if (this.m_hndFindFile != null)
                    {
                        this.m_hndFindFile.Close();
                        this.m_hndFindFile = null;
                    }

                    return this.MoveNext();
                }
            }

            return retval;
        }

        /// <summary>
        ///     Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">
        ///     The collection was modified after the enumerator was created.
        /// </exception>
        public void Reset()
        {
            this.m_hndFindFile = null;
        }

        #endregion
    }
}