using System.Collections.Generic;
using Endjin.Assembly.ChangeDetection.Infrastructure;
using Endjin.Assembly.ChangeDetection.Rules;
using Semver;

namespace Endjin.Assembly.ChangeDetection.SemVer
{
    #region Using Directives

    

    #endregion

    public class SemanticVersionAnalyzer
    {
        public AnalysisResult Analyze(string previousAssembly, string currentAssembly, string proposedVersionNumber)
        {
            var differ = new DiffAssemblies();

            var previous = new FileQuery(previousAssembly);
            var current = new FileQuery(currentAssembly);

            var differences = differ.Execute(new List<FileQuery>
            {
                previous
            }, 
            new List<FileQuery>
            {
                current
            });

            var rule = new BreakingChangeRule();

            var breakingChange = rule.Detect(differences);

            if (breakingChange)
            {
                var semVer = SemVersion.Parse(proposedVersionNumber);

                var decidedVersionNumber = semVer.Change(semVer.Major + 1, 0, 0);

                proposedVersionNumber = decidedVersionNumber.ToString();
            }

            return new AnalysisResult
            {
                BreakingChangesDetected = breakingChange,
                VersionNumber = proposedVersionNumber
            };
        }
    }
}