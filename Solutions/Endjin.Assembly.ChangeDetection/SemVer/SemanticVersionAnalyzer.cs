namespace AssemblyDifferences.SemVer
{
    #region Using Directives

    using System.Collections.Generic;

    using AssemblyDifferences.Infrastructure;
    using AssemblyDifferences.Rules;

    using Semver;

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

            var validationResult = rule.Validate(differences);

            if (!validationResult)
            {
                var semVer = SemVersion.Parse(proposedVersionNumber);

                var decidedVersionNumber = semVer.Change(semVer.Major + 1, 0, 0);

                proposedVersionNumber = decidedVersionNumber.ToString();
            }

            return new AnalysisResult
            {
                BreakingChangesDetected = validationResult,
                VersionNumber = proposedVersionNumber
            };
        }
    }
}