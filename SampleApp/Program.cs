namespace SampleApp
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Jokedst.GetOpt;

    /// <summary>
    /// Sample program that sorts a delimited file (e.g. a CSV file) based on a specific field on each row
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main entry
        /// </summary>
        /// <param name="args"> Command line parameters </param>
        public static void Main(string[] args)
        {
            var separator = "|";
            var field = 0;
            var verbose = false;
            var numeric = false;
            string file = null;

            var opts = new GetOpt(
                "Sample application that sorts input rows based on a delimeted field", 
                new[]
                    {
                        new CommandLineOption('s', "separator", "Field separator", ParameterType.String, o => separator = (string)o),
                        new CommandLineOption('v', "verbose", "Show more info about found files", ParameterType.None, o => verbose = true),
                        new CommandLineOption('V', null, "Show version", ParameterType.None, o => Console.WriteLine("Version: 1.0")),
                        new CommandLineOption('\0', "numeric", "sort numerically", ParameterType.None, o => numeric = true),
                        new CommandLineOption('f', "field", "Which field to sort by. Default = 0", ParameterType.Integer, o => field = (int)o),
                        new CommandLineOption("file", ParameterType.String, o => file = (string)o, true),
                    });

            try
            {
                opts.ParseOptions(args);
            }
            catch (CommandLineException e)
            {
                Console.WriteLine("Error: {0}", e.Message);
                return;
            }

            if (verbose) Console.WriteLine("Starting...");
            
            // Read given file or standard input, split it up according to delimiter and sort by given field. No error handling, this is an example ;)
            StreamReader input = file != null ? new StreamReader(file) : new StreamReader(Console.OpenStandardInput());
            string line;
            var s = new List<Tuple<string, string>>();
            while ((line = input.ReadLine()) != null)
            {
                var key = line.Split(separator[0])[field];
                s.Add(new Tuple<string, string>(key, line));
            }

            foreach (var linepair in numeric ? s.OrderBy(x => int.Parse(x.Item1)) : s.OrderBy(x => x.Item1))
            {
                Console.WriteLine(linepair.Item2);
            }

            if (opts.AdditionalParameters.Count > 1)
            {
                // Handle additional files here
                foreach (var additionalParameter in opts.AdditionalParameters)
                {
                    Console.WriteLine("Another parameter '{0}' was included", additionalParameter);
                }
            }
        }
    }
}
