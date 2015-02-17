﻿namespace Jokedst.GetOpt
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
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

        /// <summary>
        /// All command line options
        /// </summary>
        private readonly List<CommandLineOption> options;

        /// <summary>
        /// Lookup for all options with a short name
        /// </summary>
        private readonly Dictionary<char, CommandLineOption> shortNameLookup = new Dictionary<char, CommandLineOption>();

        /// <summary>
        /// Lookup for all options with a long name
        /// </summary>
        private readonly Dictionary<string, CommandLineOption> longNameLookup = new Dictionary<string, CommandLineOption>();

        /// <summary>
        /// List of all unnamed options (i.e. options that you write without a "-" or "--" before)
        /// </summary>
        private readonly List<CommandLineOption> unnamedList = new List<CommandLineOption>();

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
            this.options = options.ToList();
            this.AdditionalParameters = new List<string>();
            foreach (var option in this.options)
            {
                if (option.ShortName != '\0')
                {
                    this.shortNameLookup.Add(option.ShortName, option);
                }

                if (option.LongName != null)
                {
                    this.longNameLookup.Add(option.LongName, option);
                }

                if (option.ShortName == '\0' && option.LongName == null)
                {
                    this.unnamedList.Add(option);
                }
            }

            if (addHelp)
            {
                var helpOption = new CommandLineOption('h', "help", "This help", ParameterType.None, o => this.ShowUsage());
                this.shortNameLookup.Add(helpOption.ShortName, helpOption);
                this.longNameLookup.Add(helpOption.LongName, helpOption);
                this.options.Add(helpOption);
            }
        }

        /// <summary>
        /// Gets number of found and parsed options
        /// </summary>
        public int ParsedOptions { get; private set; }

        /// <summary>
        /// Gets additional parameters not specified by unnamed options
        /// </summary>
        public List<string> AdditionalParameters { get; private set; }

        /// <summary>
        /// Shows a generated help listing of all available options and parameters
        /// </summary>
        /// <param name="exitApplication">
        /// If true the application will exit after outputting the usage info
        /// </param>
        public void ShowUsage(bool exitApplication = true)
        {
            string cmd = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);

            if (this.applicationDescription != null)
            {
                Console.WriteLine(this.applicationDescription);
            }

            Console.Write("Usage: {0}", cmd);
            if (this.shortNameLookup.Count > 0)
            {
                // First take care of options without parameters
                bool firstHit = true;
                foreach (var option in this.shortNameLookup.Values)
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
                foreach (var option in this.shortNameLookup.Values)
                {
                    if (option.ParameterType != ParameterType.None)
                    {
                        Console.Write(" -{0} <{1}>", option.ShortName, option.ParameterType);
                    }
                }
            }

            // Finally write all unnamed parameters
            foreach (var option in this.unnamedList.Where(option => option.Description != null))
            {
                Console.Write(option.IsOptional ? " [{0}]" : " <{0}>", option.Description);
            }

            Console.WriteLine();

            // Now print all options, one row at a time
            if (this.shortNameLookup.Count > 0 || this.longNameLookup.Count > 0)
            {
                Console.WriteLine("Options:");

                // First create all lines so we can figure out how long each option is so it all aligns nicely
                var lines = new Dictionary<string, CommandLineOption>();
                int maxlength = 0;
                foreach (var option in this.options)
                {
                    if (option.ShortName != '\0' || option.LongName != null)
                    {
                        string line = string.Empty;
                        if (option.ShortName != '\0')
                        {
                            line += string.Format(" -{0} ", option.ShortName);
                        }
                        else
                        {
                            line += "    ";
                        }

                        if (option.LongName != null)
                        {
                            line += string.Format("--{0} ", option.LongName);
                        }

                        if (option.ParameterType != ParameterType.None)
                        {
                            line += string.Format("<{0}> ", option.ParameterType);
                        }

                        line += "{0}";
                        if (line.Length > maxlength)
                        {
                            maxlength = line.Length;
                        }

                        lines.Add(line, option);
                    }
                }

                foreach (var line in lines)
                {
                    Console.WriteLine(line.Key + ": " + line.Value.Description, string.Empty.PadLeft(maxlength - line.Key.Length));
                }
            }

            if (exitApplication)
            {
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// Parses the list of command line parameters
        /// That this function writes to the console, and may quit the program if an error occurs
        /// </summary>
        /// <param name="args">
        /// The command line parameters array to parse
        /// </param>
        /// <param name="exitOnError">
        /// If true will exit the application showing the error on any error
        /// </param>
        /// <returns>
        /// Number of parsed parameters
        /// </returns>
        public int ParseOptions(string[] args, bool exitOnError = false)
        {
            try
            {
                int i = 0;

                int unnamedIndex = 0;
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

                        if (this.longNameLookup.ContainsKey(name))
                        {
                            i = ParseAndDispatch(args, i, this.longNameLookup[name], inlineValue);
                        }
                        else
                        {
                            throw new CommandLineException(string.Format("Unknown option '{0}'", args[i]));
                        }
                    }
                    else if (args[i].StartsWith("-"))
                    {
                        int newi = i;
                        string oneLetterOptions = args[i].Substring(1), inlineValue = null;
                        if (oneLetterOptions.Contains('='))
                        {
                            inlineValue = oneLetterOptions.Substring(oneLetterOptions.IndexOf('=') + 1);
                            oneLetterOptions = oneLetterOptions.Substring(0, oneLetterOptions.IndexOf('='));
                        }

                        for (var optionIndex = 0; optionIndex < oneLetterOptions.Length; optionIndex++)
                        {
                            if (this.shortNameLookup.ContainsKey(oneLetterOptions[optionIndex]))
                            {
                                var option = this.shortNameLookup[oneLetterOptions[optionIndex]];

                                // If this is an option in the middle that requires a parameter, fail. Only the last such option can have a parameter
                                if (option.ParameterType != ParameterType.None && optionIndex != oneLetterOptions.Length - 1)
                                {
                                    throw new CommandLineException(string.Format("Option '{0}' requires a parameter", oneLetterOptions[optionIndex]));
                                }

                                newi = ParseAndDispatch(args, i, option, inlineValue);
                            }
                            else
                            {
                                throw new CommandLineException(string.Format("Unknown option '{0}'", oneLetterOptions[optionIndex]));
                            }
                        }

                        i = newi;
                    }
                    else
                    {
                        // This is an unnamed parameter
                        if (this.unnamedList.Count > unnamedIndex)
                        {
                            i = ParseAndDispatch(args, --i, this.unnamedList[unnamedIndex++]);
                        }
                        else
                        {
                            this.AdditionalParameters.Add(args[i]);
                        }
                    }

                    i++;
                }

                if (this.unnamedList.Count(x => !x.IsOptional) > unnamedIndex)
                {
                    throw new CommandLineException("Missing parameters");
                }

                this.ParsedOptions = i;
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
        /// Parses any parameters on the command line that is associated with this option, and calls the set function
        /// </summary>
        /// <param name="args">
        /// Command line arguments, usually the parameter to the "main" function
        /// </param>
        /// <param name="i">
        /// Index in the args array we should start at
        /// </param>
        /// <param name="option">
        /// Which option we are trying to parse
        /// </param>
        /// <param name="inlineValue">
        /// optional value for parameter if found
        /// </param>
        /// <returns>
        /// How many of the arguments in args we used up
        /// </returns>
        private static int ParseAndDispatch(string[] args, int i, CommandLineOption option, string inlineValue = null)
        {
            if (option.ParameterType != ParameterType.None && args.Length < i + 2 && inlineValue == null)
            {
                throw new CommandLineException(string.Format("Option '{0}' requires a parameter", option.LongName ?? option.ShortName.ToString()));
            }

            switch (option.ParameterType)
            {
                case ParameterType.Double:
                    double tempChar;
                    var ci = new CultureInfo("en-US");
                    if (!double.TryParse(inlineValue ?? args[++i], NumberStyles.Any, ci, out tempChar))
                    {
                        throw new CommandLineException(
                            string.Format("Option '{0}': '{1}' is not a valid numeric value", option.Name, inlineValue ?? args[i]));
                    }

                    if (option.SetFunction != null)
                    {
                        option.SetFunction(tempChar);
                    }

                    return i;
                case ParameterType.Integer:
                    int tempInt;
                    if (!int.TryParse(inlineValue ?? args[++i], out tempInt))
                    {
                        throw new CommandLineException(string.Format("Option '{0}': '{1}' is not a valid integer", option.Name, inlineValue ?? args[i]));
                    }

                    if (option.SetFunction != null)
                    {
                        option.SetFunction(tempInt);
                    }

                    return i;
                case ParameterType.String:
                    i++;
                    if (option.SetFunction != null)
                    {
                        option.SetFunction(inlineValue ?? args[i]);
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