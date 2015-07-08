namespace AssemblyDifferences.Introspection
{
    using System.Collections.Generic;

    internal interface ISymChkExecutor
    {
        List<string> FailedPdbs { get; set; }

        int SucceededPdbCount { get; }

        bool DownLoadPdb(string fullBinaryName, string symbolServer, string downloadDir);
    }
}