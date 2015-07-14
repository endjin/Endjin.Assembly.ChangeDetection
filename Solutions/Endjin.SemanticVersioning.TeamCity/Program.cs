namespace Endjin.SemanticVersioning.TeamCity
{
    #region Using Directives

    using System;

    using AssemblyDifferences.SemVer;

    #endregion

    public class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                var commandLineProcessor = new CommandLineProcessor();

                var options = commandLineProcessor.Process(args);

                var semanticVersionAnalyzer = new SemanticVersionAnalyzer();

                var result = semanticVersionAnalyzer.Analyze(options.PreviousAssembly, options.CurrentAssembly, options.ProposedVersionNumber);

                if (result.BreakingChangesDetected)
                {
                    Console.WriteLine("##teamcity[buildNumber '" + result.VersionNumber + "']");
                }
            }
            catch (Exception exception)
            {
                var aggregateException = exception as AggregateException;
                if (aggregateException != null)
                {
                    foreach (var e in aggregateException.InnerExceptions)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                else
                {
                    Console.WriteLine(exception.Message);
                }
            }
        }
    }
}