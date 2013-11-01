using System;

namespace SampleApp
{
    using Jokedst.GetOpt;

    public class Program
    {
        public static void Main(string[] args)
        {
            var separator = "|";
            var field = 0;
            var verbose = false;
            var numeric = false;
            string file = null;

            var opts = new GetOpt(new[] 
            {
                new CommandLineOption('s', "separator", "Field separator", ParameterType.String, o => separator = (string)o),
                new CommandLineOption('v', "verbose", "Show more info about found files", ParameterType.None, o => verbose = true),
                new CommandLineOption('n', "numeric", "sort numerically", ParameterType.None, o => numeric = true),
                new CommandLineOption('f', "field", "Which field to sort by. Default = 0", ParameterType.Integer, o => field = (int)o),
                new CommandLineOption("file name", ParameterType.String, o => file = (string)o, true), 
            });

            try
            {
                opts.ParseOptions(args);
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("Error: {0}", e.Message);
                return;
            }
        }
    }
}
