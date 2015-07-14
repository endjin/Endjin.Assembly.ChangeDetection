namespace Endjin.SemanticVersioning.TeamCity
{
    #region Using Directives

    using System;

    using global::CommandLine;

    #endregion

    public class CommandLineProcessor
    {
        public CommandOptions Process(string[] args)
        {
            var options = new CommandOptions();
            var parser = new Parser();

            if (!parser.ParseArguments(args, options))
            {
                throw new InvalidOperationException(options.GetUsage());
            }

            return options;
        }
    }
}