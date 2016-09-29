using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Endjin.Assembly.ChangeDetection.Infrastructure
{
    public class FileQuery
    {
        private readonly SearchOption mySearchOption;

        private DirectorySearcherAsync mySearcher;

        internal bool UseCwd = false;

        public FileQuery(string query) : this(query, SearchOption.TopDirectoryOnly)
        {
        }

        public FileQuery(string query, SearchOption searchOption)
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentException("The file query was null or empty");
            }

            this.mySearchOption = searchOption;
            this.Query = Environment.ExpandEnvironmentVariables(query);

            var gacidx = query.IndexOf("gac:\\", StringComparison.OrdinalIgnoreCase);
            if (gacidx == 0)
            {
                if (query.Contains("*"))
                {
                    throw new ArgumentException(string.Format("Wildcards are not supported in Global Assembly Cache search: {0}", query));
                }

                var fileName = query.Substring(5);

                var dirName = GetFileNameWithOutDllExtension(fileName);

                if (Directory.Exists(Path.Combine(GAC_32, dirName)))
                {
                    this.Query = Path.Combine(Path.Combine(GAC_32, dirName), fileName);
                }

                if (Directory.Exists(Path.Combine(GAC_MSIL, dirName)))
                {
                    this.Query = Path.Combine(Path.Combine(GAC_MSIL, dirName), fileName);
                }

                this.mySearchOption = SearchOption.AllDirectories;
            }
        }

        public FileQuery(string searchDir, string filemask)
        {
            if (string.IsNullOrEmpty(searchDir))
            {
                throw new ArgumentNullException("searchdir");
            }
            if (string.IsNullOrEmpty(filemask))
            {
                throw new ArgumentNullException("filemask");
            }

            this.Query = Path.Combine(Environment.ExpandEnvironmentVariables(searchDir), filemask);
        }

        public string SearchDir
        {
            get
            {
                // relative directory given use current working directory
                if (!this.Query.Contains('\\'))
                {
                    return Directory.GetCurrentDirectory();
                }
                if (this.Query.StartsWith("GAC:\\", StringComparison.OrdinalIgnoreCase))
                {
                    return "GAC:\\";
                }

                if (this.UseCwd)
                {
                    return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), Path.GetDirectoryName(this.Query)));
                }
                // absolute directory path is already fully specified
                return Path.GetFullPath(Path.GetDirectoryName(this.Query));
            }
        }

        public string FileMask
        {
            get
            {
                return Path.GetFileName(this.Query);
            }
        }

        public string Query { get; private set; }

        public string[] Files
        {
            get
            {
                var lret = new string[0];

                this.BeginSearch();

                if (this.mySearcher != null)
                {
                    var matches = new List<string>();
                    foreach (var file in this.mySearcher.GetResultQueue())
                    {
                        matches.Add(file);
                    }
                    lret = matches.ToArray();
                }

                return lret;
            }
        }

        public bool HasMatches
        {
            get
            {
                this.BeginSearch();
                if (this.mySearcher != null)
                {
                    return this.mySearcher.HasMatchingFiles;
                }
                return false;
            }
        }

        public BlockingQueue<string> EnumerateFiles
        {
            get
            {
                this.BeginSearch();
                return this.mySearcher.GetResultQueue();
            }
        }

        private static string GAC_32
        {
            get
            {
                return Path.Combine(Environment.GetEnvironmentVariable("WINDIR"), "assembly\\GAC_32");
            }
        }

        private static string GAC_MSIL
        {
            get
            {
                return Path.Combine(Environment.GetEnvironmentVariable("WINDIR"), "assembly\\GAC_MSIL");
            }
        }

        public void BeginSearch()
        {
            if (this.mySearcher == null && this.SearchDir != "GAC:\\" && !string.IsNullOrEmpty(this.FileMask))
            {
                this.mySearcher = new DirectorySearcherAsync(this.SearchDir, this.FileMask, this.mySearchOption);
                this.mySearcher.BeginSearch();
            }
        }

        public static List<FileQuery> ParseQueryList(string query)
        {
            return ParseQueryList(query, null, SearchOption.TopDirectoryOnly);
        }

        private static string GetFileNameWithOutDllExtension(string file)
        {
            if (file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                return file.Substring(0, file.Length - 4);
            }
            return file;
        }

        public static List<FileQuery> ParseQueryList(string query, string rootDir, SearchOption searchOption)
        {
            var ret = new List<FileQuery>();

            var queries = query.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var q in queries)
            {
                var querystr = q.Trim();
                if (!string.IsNullOrEmpty(rootDir))
                {
                    querystr = Path.Combine(rootDir, q);
                }

                ret.Add(new FileQuery(querystr, searchOption));
            }

            return ret;
        }

        public string GetMatchingFileByName(string fileName)
        {
            var pureFileName = Path.GetFileName(fileName);

            foreach (var file in this.EnumerateFiles)
            {
                if (string.Compare(Path.GetFileName(file), pureFileName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return file;
                }
            }

            return null;
        }
    }
}