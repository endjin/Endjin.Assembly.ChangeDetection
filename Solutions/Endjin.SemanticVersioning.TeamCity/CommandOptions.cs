namespace Endjin.SemanticVersioning.TeamCity
{    
    #region Using Directives

    using System;

    using CommandLine;
    using CommandLine.Text;

    #endregion 

    public class CommandOptions
    {
        [Option('s', "description", DefaultValue = false, HelpText = "Help Text")]
        public bool EnableSlowCheetahSupport { get; set; }

        public string PreviousAssembly { get; set; }

        public string CurrentAssembly { get; set; }

        public string ProposedVersionNumber { get; set; }

        [HelpOption(HelpText = "Display this help text.")]
        public string GetUsage()
        {
            var help = new HelpText(new HeadingInfo("DeployFx for Windows Azure", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()))
            {
                AdditionalNewLineAfterOption = false,
                MaximumDisplayWidth = Console.WindowWidth,
                Copyright = new CopyrightInfo("Endjin", DateTime.Now.Year)
            };

            help.AddPreOptionsLine("Usage:");
            help.AddPreOptionsLine(@"    ConfigureFx.AzureConfigTokenizer.exe -f <PATH>\ServiceConfiguration.Cloud.cscfg -c <PATH>\ServiceConfiguration.cscfg -e <PATH>\environmentConfig.xml -s");
            help.AddOptions(this);

            return help;
        } 
    }
}