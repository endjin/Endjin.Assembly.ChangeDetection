namespace Endjin.SemanticVersioning.TeamCity
{    
    #region Using Directives

    using System;

    using CommandLine;
    using CommandLine.Text;

    #endregion 

    public class CommandOptions
    {
        [Option('p', "previous assembly", HelpText = "Help Text")]
        public string PreviousAssembly { get; set; }

        [Option('c', "current assembly", HelpText = "Help Text")]
        public string CurrentAssembly { get; set; }

        [Option('v', "proposed version number", HelpText = "Help Text")]
        public string ProposedVersionNumber { get; set; }

        [Option('o', "output file path", HelpText = "Help Text")]
        public string OutputFile { get; set; }

        [HelpOption(HelpText = "Display this help text.")]
        public string GetUsage()
        {
            var help = new HelpText(new HeadingInfo("Semantic Versioning", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()))
            {
                AdditionalNewLineAfterOption = false,
                MaximumDisplayWidth = Console.WindowWidth,
                Copyright = new CopyrightInfo("Endjin", DateTime.Now.Year)
            };

            help.AddPreOptionsLine("Usage:");
            help.AddPreOptionsLine(@"    Endjin.SemanticVersioning.TeamCity.exe -o <PATH>\foo.final.dll -p <PATH>\foo.dll -c <PATH>\foo.dll -v '2.0.0'");
            help.AddOptions(this);

            return help;
        } 
    }
}