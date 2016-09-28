using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Endjin.Assembly.ChangeDetection.Infrastructure;

namespace Endjin.Assembly.ChangeDetection.Introspection
{
    internal class SymChkExecutor : ISymChkExecutor
    {
        private static readonly TypeHashes myType = new TypeHashes(typeof(SymChkExecutor));

        internal static bool bCanStartSymChk = true;

        private static readonly Regex symPassedFileCountParser = new Regex(@"SYMCHK: PASSED \+ IGNORED files = (?<succeeded>\d+) *", RegexOptions.IgnoreCase);

        private static readonly Regex symFailedFileParser = new Regex(@"SYMCHK: (?<filename>.*?) +FAILED", RegexOptions.IgnoreCase);

        internal string SymChkExeName = "symchk.exe";

        public SymChkExecutor()
        {
            this.FailedPdbs = new List<string>();
        }

        public int SucceededPdbCount { get; private set; }

        public List<string> FailedPdbs { get; set; }

        public bool DownLoadPdb(string fullbinaryName, string symbolServer, string downloadDir)
        {
            using (var t = new Tracer(myType, "DownLoadPdb"))
            {
                var lret = bCanStartSymChk;

                if (lret)
                {
                    var startInfo = new ProcessStartInfo(this.SymChkExeName, this.BuildCmdLine(fullbinaryName, symbolServer, downloadDir));

                    startInfo.RedirectStandardOutput = true;
                    startInfo.RedirectStandardError = true;
                    startInfo.UseShellExecute = false;
                    startInfo.CreateNoWindow = true;

                    Process proc = null;
                    try
                    {
                        proc = Process.Start(startInfo);
                        proc.OutputDataReceived += this.proc_OutputDataReceived;
                        proc.ErrorDataReceived += this.proc_OutputDataReceived;
                        proc.BeginErrorReadLine();
                        proc.BeginOutputReadLine();

                        proc.WaitForExit();
                    }
                    catch (Win32Exception ex)
                    {
                        bCanStartSymChk = false;
                        t.Error(ex, "Could not start symchk.exe to download pdb files");
                        lret = false;
                    }
                    finally
                    {
                        if (proc != null)
                        {
                            proc.OutputDataReceived -= this.proc_OutputDataReceived;
                            proc.ErrorDataReceived -= this.proc_OutputDataReceived;
                            proc.Dispose();
                        }
                    }
                }

                if (this.FailedPdbs.Count > 0)
                {
                    lret = false;
                }

                return lret;
            }
        }

        internal string BuildCmdLine(string binaryFileName, string symbolServer, string downloadDir)
        {
            var lret = string.Format("\"{0}\" /su \"{1}\" /oc \"{2}\"", binaryFileName, symbolServer, downloadDir ?? Path.GetDirectoryName(binaryFileName));

            Tracer.Info(Level.L1, myType, "BuildCmdLine", "Symcheck command is {0} {1}", this.SymChkExeName, lret);

            return lret;
        }

        private void proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                var line = e.Data;

                var m = symPassedFileCountParser.Match(line);
                if (m.Success)
                {
                    lock (this)
                    {
                        this.SucceededPdbCount += int.Parse(m.Groups["succeeded"].Value, CultureInfo.InvariantCulture);
                    }
                }

                m = symFailedFileParser.Match(line);
                if (m.Success)
                {
                    lock (this)
                    {
                        this.FailedPdbs.Add(m.Groups["filename"].Value);
                    }
                }
            }
        }
    }
}