using Endjin.Assembly.ChangeDetection.SemVer;

namespace Endjin.SemanticVersioning.TeamCity
{
    #region Using Directives

    using System;
    using ILMerging;

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
                    var dynamicAssembly = new CodeGenerator().GenerateVersionDetailsDynamicAssembly(options.CurrentAssembly, result.VersionNumber, result.VersionNumber, result.VersionNumber);

                    var ilMerge = new ILMerge
                    {
                        TargetKind = ILMerge.Kind.Dll,
                        AttributeFile = dynamicAssembly,
                        OutputFile = options.OutputFile
                    };

                    ilMerge.SetInputAssemblies(new[] { options.CurrentAssembly } );
                    ilMerge.Merge();

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