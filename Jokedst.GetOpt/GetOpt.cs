namespace Jokedst.GetOpt
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    /// <summary>
    ///     Handles command line parameters, including loading them from a file with "-f" parameter and showing help with "-h"
    ///     parameter
    /// </summary>
    public class GetOpt
    {
        /// <summary>
        ///     The application description, used in ShowUsage.
        /// </summary>
        private readonly string applicationDescription;

        /// <summary>
        ///     Lookup for all options with a long name
        /// </summary>
        private readonly Dictionary<string, CommandLineOption> longNameLookup =
            new Dictionary<string, CommandLineOption>();

        /// <summary>
        ///     All command line options
        /// </summary>
        private readonly List<CommandLineOption> options;

        /// <summary>
        ///     Lookup for all options with a short name
        /// </summary>
        private readonly Dictionary<char, CommandLineOption> shortNameLookup = new Dictionary<char, CommandLineOption>()
            ;

        /// <summary>
        ///     List of all unnamed options (i.e. options that you write without a "-" or "--" before)
        /// </summary>
        private readonly List<CommandLineOption> unnamedList = new List<CommandLineOption>();

        /// <summary>
        ///     Initializes a new instance of the <see cref="GetOpt" /> class
        /// </summary>
        /// <param name="applicationDescription">
        ///     Description of this application, to be used in ShowUsage
        /// </param>
        /// <param name="options">
        ///     The list of options that should be parsed for
        /// </param>
        /// <param name="addHelp">
        ///     If true (default) a help option will be added automatically
        /// </param>
        public GetOpt(string applicationDescription, IEnumerable<CommandLineOption> options, bool addHelp = true)
        {
            this.applicationDescription = applicationDescription;
            ParsedOptions = -1;
            this.options = options.ToList();
            AdditionalParameters = new List<string>();
            foreach (var option in this.options)
            {
                if (option.ShortName != '\0')
                    shortNameLookup.Add(option.ShortName, option);

                if (option.LongName != null)
                    longNameLookup.Add(option.LongName, option);

                if (option.ShortName == '\0' && option.LongName == null)
                    unnamedList.Add(option);
            }

            if (addHelp)
            {
                var helpOption = new CommandLineOption('h', "help", "This help", ParameterType.None, o => ShowUsage());
                shortNameLookup.Add(helpOption.ShortName, helpOption);
                longNameLookup.Add(helpOption.LongName, helpOption);
                this.options.Add(helpOption);
            }
        }

        /// <summary>
        ///     Gets number of found and parsed options
        /// </summary>
        public int ParsedOptions { get; private set; }

        /// <summary>
        ///     Gets additional parameters not specified by unnamed options
        /// </summary>
        public List<string> AdditionalParameters { get; }

        /// <summary>
        ///     Shows a generated help listing of all available options and parameters
        /// </summary>
        /// <param name="exitApplication">
        ///     If true the application will exit after outputting the usage info
        /// </param>
        public void ShowUsage(bool exitApplication = true)
        {
            var cmd = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);

            if (applicationDescription != null)
                Console.WriteLine(applicationDescription);

            Console.Write("Usage: {0}", cmd);
            if (shortNameLookup.Count > 0)
            {
                // First take care of options without parameters
                var firstHit = true;
                foreach (var option in shortNameLookup.Values)
                    if (option.ParameterType == ParameterType.None)
                    {
                        if (firstHit)
                        {
                            Console.Write(" -");
                            firstHit = false;
                        }

                        Console.Write(option.ShortName);
                    }

                // Then take care of options with parameters
                foreach (var option in shortNameLookup.Values)
                    if (option.ParameterType != ParameterType.None)
                        Console.Write(" -{0} <{1}>", option.ShortName,
                            option.LongName ?? option.ParameterType.ToString());
            }

            // Finally write all unnamed parameters
            foreach (var option in unnamedList.Where(option => option.Description != null))
                Console.Write(option.IsOptional ? " [{0}]" : " <{0}>", option.Description);

            Console.WriteLine();

            // Now print all options, one row at a time
            if (shortNameLookup.Count > 0 || longNameLookup.Count > 0)
            {
                Console.WriteLine("Options:");

                // First create all lines so we can figure out how long each option is so it all aligns nicely
                var lines = new Dictionary<string, CommandLineOption>();
                var maxlength = 0;
                foreach (var option in options)
                    if (option.ShortName != '\0' || option.LongName != null)
                    {
                        var line = string.Empty;
                        if (option.ShortName != '\0')
                            line += $" -{option.ShortName} ";
                        else
                            line += "    ";

                        if (option.LongName != null)
                            line += $"--{option.LongName} ";

                        if (option.ParameterType != ParameterType.None)
                            line += $"<{option.ParameterType}> ";

                        line += "{0}";
                        if (line.Length > maxlength)
                            maxlength = line.Length;

                        lines.Add(line, option);
                    }

                foreach (var line in lines)
                    Console.WriteLine(line.Key + ": " + line.Value.Description,
                        string.Empty.PadLeft(maxlength - line.Key.Length));
            }

            if (exitApplication)
                Environment.Exit(0);
        }

        /// <summary>
        ///     Parses the list of command line parameters
        ///     That this function writes to the console, and may quit the program if an error occurs
        /// </summary>
        /// <param name="args">
        ///     The command line parameters array to parse
        /// </param>
        /// <param name="exitOnError">
        ///     If true will exit the application showing the error on any error
        /// </param>
        /// <returns>
        ///     Number of parsed parameters
        /// </returns>
        public int ParseOptions(string[] args, bool exitOnError = false)
        {
            try
            {
                var i = 0;

                var unnamedIndex = 0;
                while (i < args.Length)
                {
                    if (args[i].StartsWith("--"))
                    {
                        string name = args[i].Substring(2), inlineValue = null;
                        if (name.Contains('='))
                        {
                            inlineValue = name.Substring(name.IndexOf('=') + 1);
                            name = name.Substring(0, name.IndexOf('='));
                        }

                        if (longNameLookup.ContainsKey(name))
                            i = ParseAndDispatch(args, i, longNameLookup[name], inlineValue);
                        else
                            throw new CommandLineException($"Unknown option '{args[i]}'");
                    }
                    else if (args[i].StartsWith("-"))
                    {
                        var newi = i;
                        string oneLetterOptions = args[i].Substring(1), inlineValue = null;
                        if (oneLetterOptions.Contains('='))
                        {
                            inlineValue = oneLetterOptions.Substring(oneLetterOptions.IndexOf('=') + 1);
                            oneLetterOptions = oneLetterOptions.Substring(0, oneLetterOptions.IndexOf('='));
                        }

                        for (var optionIndex = 0; optionIndex < oneLetterOptions.Length; optionIndex++)
                            if (shortNameLookup.ContainsKey(oneLetterOptions[optionIndex]))
                            {
                                var option = shortNameLookup[oneLetterOptions[optionIndex]];

                                // If this is an option in the middle that requires a parameter, fail. Only the last such option can have a parameter
                                if (option.ParameterType != ParameterType.None &&
                                    optionIndex != oneLetterOptions.Length - 1)
                                    throw new CommandLineException(
                                        $"Option '{oneLetterOptions[optionIndex]}' requires a parameter");

                                newi = ParseAndDispatch(args, i, option, inlineValue);
                            }
                            else
                            {
                                throw new CommandLineException($"Unknown option '{oneLetterOptions[optionIndex]}'");
                            }

                        i = newi;
                    }
                    else
                    {
                        // This is an unnamed parameter
                        if (unnamedList.Count > unnamedIndex)
                            i = ParseAndDispatch(args, --i, unnamedList[unnamedIndex++]);
                        else
                            AdditionalParameters.Add(args[i]);
                    }

                    i++;
                }

                if (unnamedList.Count(x => !x.IsOptional) > unnamedIndex)
                    throw new CommandLineException("Missing parameters");

                ParsedOptions = i;
                return i;
            }
            catch (CommandLineException e)
            {
                if (exitOnError)
                {
                    Console.WriteLine("Error: {0}", e.Message);
                    Environment.Exit(1);
                    return 0;
                }

                throw;
            }
        }

        /// <summary>
        ///     Parses any parameters on the command line that is associated with this option, and calls the set function
        /// </summary>
        /// <param name="args">
        ///     Command line arguments, usually the parameter to the "main" function
        /// </param>
        /// <param name="i">
        ///     Index in the args array we should start at
        /// </param>
        /// <param name="option">
        ///     Which option we are trying to parse
        /// </param>
        /// <param name="inlineValue">
        ///     optional value for parameter if found
        /// </param>
        /// <returns>
        ///     How many of the arguments in args we used up
        /// </returns>
        private static int ParseAndDispatch(string[] args, int i, CommandLineOption option, string inlineValue = null)
        {
            if (option.ParameterType != ParameterType.None && args.Length < i + 2 && inlineValue == null)
                throw new CommandLineException(
                    $"Option '{option.LongName ?? option.ShortName.ToString()}' requires a parameter");

            switch (option.ParameterType)
            {
                case ParameterType.Double:
                    var ci = new CultureInfo("en-US");
                    if (!double.TryParse(inlineValue ?? args[++i], NumberStyles.Any, ci, out var tempChar))
                        throw new CommandLineException(
                            $"Option '{option.Name}': '{inlineValue ?? args[i]}' is not a valid numeric value");

                    option.SetFunction?.Invoke(tempChar);

                    return i;
                case ParameterType.Integer:
                    if (!int.TryParse(inlineValue ?? args[++i], out var tempInt))
                        throw new CommandLineException(
                            $"Option '{option.Name}': '{inlineValue ?? args[i]}' is not a valid integer");

                    option.SetFunction?.Invoke(tempInt);

                    return i;
                case ParameterType.String:
                    i++;
                    option.SetFunction?.Invoke(inlineValue ?? args[i]);

                    return i;
                case ParameterType.None:
                    option.SetFunction?.Invoke(null);

                    return i;
            }

            return i;
        }

        // Fluent interface

        public static GetOpt Description(string description)
        {
            return new GetOpt(description, new List<CommandLineOption>());
        }

        public GetOpt Parameter(char shortName, string longName, Action onFlagDetected, string description = null)
        {
            options.Add(new CommandLineOption(shortName, longName, description, ParameterType.None, o => onFlagDetected()));
            return this;
        }

        public GetOpt Parameter(char shortName, string longName, Action<string> setParameter, string description = null)
        {
            options.Add(new CommandLineOption(shortName, longName, description, ParameterType.String, o => setParameter(o as string)));
            return this;
        }

        public GetOpt Parameter(char shortName, string longName, Action<int> setParameter, string description = null)
        {
            options.Add(new CommandLineOption(shortName, longName, description, ParameterType.Integer, o => setParameter((int) o)));
            return this;
        }

        public GetOpt Parameter(Action<string> setParameter, string description = null)
        {
            options.Add(new CommandLineOption(description, ParameterType.String, o => setParameter(o as string)));
            return this;
        }

        //public GetOpt Add<T>(char shortName, string longName, ref T target)
        //{
        //    options.Add(new CommandLineOption(shortName, longName, null, ParameterType.String,
        //        value => { TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(value); }));
        //    return this;
        //}
    }
}