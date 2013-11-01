namespace Jokedst.GetOpt
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    public enum ParameterType
    {
        None,
        Integer,
        String,
        Double
    }

    public delegate void SetOptionDelegate(Object value);

    /// <summary>
    /// Describes a possible option that will be parsed from the command line parameters
    /// </summary>
    public class CommandLineOption
    {
        /// <summary>Single character version of this option (e.g. -f)</summary>
        public char ShortName = '\0';
        /// <summary>Long version of this option (e.g. --file)</summary>
        public string LongName;
        /// <summary>A description that will be shown in the usage. Also the (optional) name of a unnamed parameter</summary>
        public string Description;
        /// <summary>Type of value this parameter accepts</summary>
        public ParameterType ParameterType;
        /// <summary>A function that will be called when this option has been parsed</summary>
        public SetOptionDelegate SetFunction;

        public bool IsOptional = true;

        public CommandLineOption() { }
        public CommandLineOption(char shortName, string longName, string description, ParameterType parameterType, SetOptionDelegate setFunction)
        {
            ShortName = shortName;
            LongName = longName;
            Description = description;
            ParameterType = parameterType;
            SetFunction = setFunction;
        }

        public CommandLineOption(string description, ParameterType parameterType, SetOptionDelegate setFunction, bool optional)
        {
            Description = description;
            ParameterType = parameterType;
            SetFunction = setFunction;
            IsOptional = optional;
        }

        /// <summary>Internal helper function to show the name of this option, regardless of what type it is</summary>
        internal string Name
        {
            get
            {
                if (LongName != null) return LongName;
                if (ShortName != '\0') return "-" + ShortName;
                return Description;
            }
        }
    }

    /// <summary>
    /// Handles command line parameters, including loading them from a file with "-f" parameter and showing help with "-h" parameter
    /// </summary>
    public class GetOpt
    {
        private readonly IEnumerable<CommandLineOption> _options;
        private readonly Dictionary<char, CommandLineOption> _shortNameLookup = new Dictionary<char, CommandLineOption>();
        private readonly Dictionary<string, CommandLineOption> _longNameLookup = new Dictionary<string, CommandLineOption>();
        private readonly List<CommandLineOption> _unnamedList = new List<CommandLineOption>();
        public int ParsedOptions = -1;

        /// <summary>
        /// Shows a generated help listing of all available options and parameters
        /// </summary>
        public void ShowUsage()
        {
            string cmd = Environment.CommandLine;
            if (cmd.IndexOf(" ") != -1) cmd = cmd.Substring(0, cmd.IndexOf(" "));
            if (cmd.EndsWith(".exe", true, null)) cmd = cmd.Substring(0, cmd.Length - 4);
            Console.Write("Usage: {0}", cmd);
            if (_shortNameLookup.Count > 0)
            {
                //First take care of options without parameters
                bool firstHit = true;
                foreach (var option in _shortNameLookup.Values)
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

                //Then take care of options with parameters
                foreach (var option in _shortNameLookup.Values)
                {
                    if (option.ParameterType != ParameterType.None)
                    {
                        Console.Write(" -{0} <{1}>", option.ShortName, option.ParameterType);
                    }
                }
            }
            //Finally write all unnamed parameters
            foreach (var option in _unnamedList)
            {
                if (option.Description != null)
                    Console.Write(" <{0}>", option.Description);
            }
            Console.WriteLine();

            //Now print all options, one row at a time
            if (_shortNameLookup.Count > 0 || _longNameLookup.Count > 0)
            {
                Console.WriteLine("Options:");
                var lines = new Dictionary<string, CommandLineOption>();
                int maxlength = 0;
                foreach (var option in _options)
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
                    Console.WriteLine(line.Key + ": " + line.Value.Description, "".PadLeft(maxlength - line.Key.Length));
                }
            }
            Environment.Exit(0);
        }

        /// <summary>
        /// Parses any parameters on the command line that is associated with this option, and calls the set function
        /// </summary>
        /// <param name="args"></param>
        /// <param name="i"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        private static int ParseAndDispatch(string[] args, int i, CommandLineOption option)
        {
            if (option.ParameterType != ParameterType.None && args.Length < i + 2)
                throw new ArgumentException(string.Format("Option '{0}' requires a parameter", option.LongName ?? option.ShortName.ToString()));

            switch (option.ParameterType)
            {
                case ParameterType.Double:
                    double tempChar;
                    var ci = new CultureInfo("en-US");
                    if (!double.TryParse(args[++i], NumberStyles.Any, ci, out tempChar))
                        throw new ArgumentException(string.Format("Option '{0}': '{1}' is not a valid numeric value", option.Name, args[i]));
                    if (option.SetFunction != null)
                        option.SetFunction(tempChar);
                    return i;
                case ParameterType.Integer:
                    int tempInt;
                    if (!int.TryParse(args[++i], out tempInt))
                        throw new ArgumentException(string.Format("Option '{0}': '{1}' is not a valid integer", option.Name, args[i]));
                    if (option.SetFunction != null)
                        option.SetFunction(tempInt);
                    return i;
                case ParameterType.String:
                    if (option.SetFunction != null)
                        option.SetFunction(args[++i]);
                    return i;
                case ParameterType.None:
                    if (option.SetFunction != null)
                        option.SetFunction(null);
                    return i;
            }
            return i;
        }

        /// <summary>
        /// Initiates the GetOpt object with the options available
        /// </summary>
        /// <param name="options">The list of options that should be parsed for</param>
        public GetOpt(IEnumerable<CommandLineOption> options)
        {
            _options = options;
            foreach (var option in options)
            {
                if (option.ShortName != '\0')
                    _shortNameLookup.Add(option.ShortName, option);
                if (option.LongName != null)
                    _longNameLookup.Add(option.LongName, option);
                if (option.ShortName == '\0' && option.LongName == null)
                    _unnamedList.Add(option);
            }
            var helpOption = new CommandLineOption
            {
                LongName = "help",
                ShortName = 'h',
                Description = "This help",
                SetFunction = o => ShowUsage()
            };
            _shortNameLookup.Add(helpOption.ShortName, helpOption);
            _longNameLookup.Add(helpOption.LongName, helpOption);
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
                    if (_longNameLookup.ContainsKey(name))
                    {
                        i = ParseAndDispatch(args, i, _longNameLookup[name]);
                    }
                    else
                        throw new ArgumentException(string.Format("Unknown option '{0}'", args[i]));
                }
                else if (args[i].StartsWith("-"))
                {
                    int newi = i;
                    for (var optionIndex = 1; optionIndex < args[i].Length; optionIndex++)
                    {
                        if (_shortNameLookup.ContainsKey(args[i][optionIndex]))
                        {
                            var option = _shortNameLookup[args[i][optionIndex]];
                            //If this is an option in the middle that requires a parameter, fail. Only the last such option can have a parameter
                            if (option.ParameterType != ParameterType.None && optionIndex != args[i].Length - 1)
                                throw new ArgumentException("Option '{0}' requires a parameter", args[i][optionIndex].ToString());
                            newi = ParseAndDispatch(args, i, option);
                        }
                        else
                            throw new ArgumentException(string.Format("Unknown option '{0}'", args[i][optionIndex].ToString()));
                    }

                    i = newi;
                }
                else
                {
                    //This is an unnamed parameter
                    if (_unnamedList.Count > unnamedIndex)
                    {
                        i = ParseAndDispatch(args, --i, _unnamedList[unnamedIndex++]);
                    }
                }
                i++;
            }
            if (_unnamedList.Where(x => !x.IsOptional).Count() != unnamedIndex)
                throw new ArgumentException("Missing parameters");

            ParsedOptions = i;
            return i;
        }
    }
}