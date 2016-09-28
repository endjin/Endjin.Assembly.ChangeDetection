using System.Collections.Generic;

namespace Endjin.Assembly.ChangeDetection.Introspection
{
    internal interface ISymChkExecutor
    {
        List<string> FailedPdbs { get; set; }

        int SucceededPdbCount { get; }

        bool DownLoadPdb(string fullBinaryName, string symbolServer, string downloadDir);
    }
}