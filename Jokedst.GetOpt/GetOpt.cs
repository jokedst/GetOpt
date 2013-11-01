namespace Jokedst.GetOpt
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Handles command line parameters, including loading them from a file with "-f" parameter and showing help with "-h" parameter
    /// </summary>
    public class GetOpt
    {
        /// <summary>
        /// The application description, used in ShowUsage.
        /// </summary>
        private readonly string applicationDescription;

        private readonly List<CommandLineOption> _options;

        private readonly Dictionary<char, CommandLineOption> _shortNameLookup = new Dictionary<char, CommandLineOption>();

        private readonly Dictionary<string, CommandLineOption> _longNameLookup = new Dictionary<string, CommandLineOption>();

        private readonly List<CommandLineOption> _unnamedList = new List<CommandLineOption>();

        /// <summary>
        /// Initializes a new instance of the <see cref="GetOpt"/> class 
        /// </summary>
        /// <param name="applicationDescription">
        /// Description of this application, to be used in ShowUsage
        /// </param>
        /// <param name="options">
        /// The list of options that should be parsed for
        /// </param>
        /// <param name="addHelp">
        /// If true (default) a help option will be added automatically
        /// </param>
        public GetOpt(string applicationDescription, IEnumerable<CommandLineOption> options, bool addHelp = true)
        {
            this.applicationDescription = applicationDescription;
            this.ParsedOptions = -1;
            this._options = options.ToList();
            foreach (var option in this._options)
            {
                if (option.ShortName != '\0')
                    this._shortNameLookup.Add(option.ShortName, option);
                if (option.LongName != null)
                    this._longNameLookup.Add(option.LongName, option);
                if (option.ShortName == '\0' && option.LongName == null)
                    this._unnamedList.Add(option);
            }

            if (addHelp)
            {
                var helpOption = new CommandLineOption('h', "help", "This help", ParameterType.None, o => this.ShowUsage());
                this._shortNameLookup.Add(helpOption.ShortName, helpOption);
                this._longNameLookup.Add(helpOption.LongName, helpOption);
                this._options.Add(helpOption);
            }
        }

        /// <summary>
        /// Gets number of found and parsed options
        /// </summary>
        public int ParsedOptions { get; private set; }

        /// <summary>
        /// Shows a generated help listing of all available options and parameters
        /// </summary>
        public void ShowUsage()
        {
            string cmd = Environment.CommandLine;
            if (cmd.IndexOf(" ") != -1)
            {
                cmd = cmd.Substring(0, cmd.IndexOf(" "));
            }

            if (cmd.EndsWith(".exe", true, null))
            {
                cmd = cmd.Substring(0, cmd.Length - 4);
            }

            if (this.applicationDescription != null)
            {
                Console.WriteLine(this.applicationDescription);
            }

            Console.Write("Usage: {0}", cmd);
            if (this._shortNameLookup.Count > 0)
            {
                // First take care of options without parameters
                bool firstHit = true;
                foreach (var option in this._shortNameLookup.Values)
                {
                    if (option.ParameterType == ParameterType.None)
                    {
                        if (firstHit)
                        {
                            Console.Write(" -");
                            firstHit = false;
                        }
                        Console.Write(option.ShortName);
                    }
                }

                // Then take care of options with parameters
                foreach (var option in this._shortNameLookup.Values)
                {
                    if (option.ParameterType != ParameterType.None)
                    {
                        Console.Write(" -{0} <{1}>", option.ShortName, option.ParameterType);
                    }
                }
            }

            // Finally write all unnamed parameters
            foreach (var option in this._unnamedList)
            {
                if (option.Description != null)
                {
                    if (option.IsOptional)
                    {
                        Console.Write(" [{0}]", option.Description);
                    }
                    else
                    {
                        Console.Write(" <{0}>", option.Description);
                    }
                }
            }

            Console.WriteLine();

            // Now print all options, one row at a time
            if (this._shortNameLookup.Count > 0 || this._longNameLookup.Count > 0)
            {
                Console.WriteLine("Options:");

                // First create all lines so we can figure out how long each option is so it all aligns nicely
                var lines = new Dictionary<string, CommandLineOption>();
                int maxlength = 0;
                foreach (var option in this._options)
                {
                    if (option.ShortName != '\0' || option.LongName != null)
                    {
                        string line = string.Empty;
                        if (option.ShortName != '\0') line += string.Format(" -{0} ", option.ShortName); else line += "   ";
                        if (option.LongName != null) line += string.Format("--{0} ", option.LongName);
                        if (option.ParameterType != ParameterType.None) line += string.Format("<{0}> ", option.ParameterType);
                        line += "{0}";
                        if (line.Length > maxlength) maxlength = line.Length;
                        ////if (option.description != null) line += string.Format(": {0}", option.description);

                        lines.Add(line, option);
                        /*
                        if (option.shortName != '\0') Console.Write(" -{0} ", option.shortName); else Console.Write("   ");
                        if (option.longName != null) Console.Write("--{0} ", option.longName); else Console.Write("    ");
                        if (option.parameterType != ParameterType.None) Console.Write("<{0}> ", option.parameterType);
                        if (option.description != null) Console.Write(": {0}", option.description);
                        Console.WriteLine();*/
                    }
                }

                foreach (var line in lines)
                {
                    Console.WriteLine(line.Key + ": " + line.Value.Description, string.Empty.PadLeft(maxlength - line.Key.Length));
                }
            }
            Environment.Exit(0);
        }

        /// <summary>
        /// Parses the list of command line parameters
        /// That this function writes to the console, and may quit the program if an error occurs
        /// </summary>
        /// <param name="args">The command line parameters array to parse</param>
        /// <returns>Number of parsed parameters</returns>
        public int ParseOptions(string[] args)
        {
            int i = 0;

            int unnamedIndex = 0;
            while (i < args.Length)
            {
                if (args[i].StartsWith("--"))
                {
                    string name = args[i].Substring(2);
                    if (this._longNameLookup.ContainsKey(name))
                    {
                        i = ParseAndDispatch(args, i, this._longNameLookup[name]);
                    }
                    else
                    {
                        throw new CommandLineException(string.Format("Unknown option '{0}'", args[i]));
                    }
                }
                else if (args[i].StartsWith("-"))
                {
                    int newi = i;
                    for (var optionIndex = 1; optionIndex < args[i].Length; optionIndex++)
                    {
                        if (this._shortNameLookup.ContainsKey(args[i][optionIndex]))
                        {
                            var option = this._shortNameLookup[args[i][optionIndex]];

                            // If this is an option in the middle that requires a parameter, fail. Only the last such option can have a parameter
                            if (option.ParameterType != ParameterType.None && optionIndex != args[i].Length - 1)
                            {
                                throw new CommandLineException("Option '{0}' requires a parameter", args[i][optionIndex].ToString());
                            }

                            newi = ParseAndDispatch(args, i, option);
                        }
                        else
                        {
                            throw new CommandLineException(string.Format("Unknown option '{0}'", args[i][optionIndex].ToString()));
                        }
                    }

                    i = newi;
                }
                else
                {
                    // This is an unnamed parameter
                    if (this._unnamedList.Count > unnamedIndex)
                    {
                        i = ParseAndDispatch(args, --i, this._unnamedList[unnamedIndex++]);
                    }
                }

                i++;
            }

            if (this._unnamedList.Count(x => !x.IsOptional) != unnamedIndex)
            {
                throw new CommandLineException("Missing parameters");
            }

            this.ParsedOptions = i;
            return i;
        }

        /// <summary>
        /// Parses any parameters on the command line that is associated with this option, and calls the set function
        /// </summary>
        /// <param name="args">Command line arguments, usually the parameter to the "main" function</param>
        /// <param name="i">Index in the args array we should start at</param>
        /// <param name="option">Which option we are trying to parse</param>
        /// <returns>How many of the arguments in args we used up</returns>
        private static int ParseAndDispatch(string[] args, int i, CommandLineOption option)
        {
            if (option.ParameterType != ParameterType.None && args.Length < i + 2)
            {
                throw new CommandLineException(string.Format("Option '{0}' requires a parameter", option.LongName ?? option.ShortName.ToString()));
            }

            switch (option.ParameterType)
            {
                case ParameterType.Double:
                    double tempChar;
                    var ci = new CultureInfo("en-US");
                    if (!double.TryParse(args[++i], NumberStyles.Any, ci, out tempChar))
                    {
                        throw new CommandLineException(
                            string.Format("Option '{0}': '{1}' is not a valid numeric value", option.Name, args[i]));
                    }

                    if (option.SetFunction != null)
                    {
                        option.SetFunction(tempChar);
                    }

                    return i;
                case ParameterType.Integer:
                    int tempInt;
                    if (!int.TryParse(args[++i], out tempInt))
                    {
                        throw new CommandLineException(string.Format("Option '{0}': '{1}' is not a valid integer", option.Name, args[i]));
                    }

                    if (option.SetFunction != null)
                    {
                        option.SetFunction(tempInt);
                    }

                    return i;
                case ParameterType.String:
                    if (option.SetFunction != null)
                    {
                        option.SetFunction(args[++i]);
                    }

                    return i;
                case ParameterType.None:
                    if (option.SetFunction != null)
                    {
                        option.SetFunction(null);
                    }
                    return i;
            }
            return i;
        }
    }
}