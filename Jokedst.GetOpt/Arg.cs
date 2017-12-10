namespace Jokedst.GetOpt
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary> A hands-off implementation of GetOpt. You just give you local values the right parameter </summary>
    public class Arg
    {
        private static readonly List<string> Arguments;
        private static readonly string ExeFileName;

        static Arg()
        {
            Arguments = Environment.GetCommandLineArgs().ToList();
            ExeFileName = Arguments[0];
            Arguments.RemoveAt(0);
        }

        public static LazyImplicit<string> NextArgument()
        {
            // If parameters look like "-f hello" we don't know if the "hello" belongs to the "-f" or not
            // until that flag is used. So we postpone the evaluation of this as long as possible
            return new LazyImplicit<string>(() => Arguments.First(x => !x.StartsWith("-")));
        }

        public static bool Flag(char flagChar)
        {
            for (var i = 0; i < Arguments.Count; i++)
            {
                if(Arguments[i].Length<2 || Arguments[i][0] != '-'||Arguments[i][1]=='-')continue;
                if (Arguments[i] == "-" + flagChar)
                {
                    // we blank this one out now that we know it's a flag and not a parameter
                    Arguments[i] = null;
                    return false;
                }
                if (Arguments[i].Contains(flagChar)) // multi-flag, e.g. "-xfg"
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets a 
        /// </summary>
        public static string Parameter(char prefixChar)
        {
            var prefix = "-" + prefixChar;
            for (var i = 0; i < Arguments.Count; i++)
            {
                if (Arguments[i] == prefix)
                {
                    if (Arguments.Count == i + 1)
                        Error($"Parameter {prefix} is not followed by a value", Arguments[i]);
                    else if (Arguments[i + 1].StartsWith("-"))
                        Error($"Parameter {prefix} is followed by {Arguments[i + 1]}, not a value", Arguments[i]);
                    else
                    {
                        // we blank this one out now that we know it's a parameter and not a flag
                        var result = Arguments[i + 1];
                        Arguments[i] = null;
                        Arguments[i + 1] = null;
                        return result;
                    }
                }
            }

            return null;
        }

        protected static void ExitProccessErrorHandler(string message, string faultyArgument)
        {
            var exeFile = Path.GetFileName(ExeFileName);
            Console.Error.WriteLine($"Error in {exeFile}:");
            Console.Error.WriteLine(message);
            Environment.Exit(-1);
        }

        public delegate void ErrorHandlerDelegate(string message, string faultyArgument);

        /// <summary>
        /// Handles found errors. Default is exit process with exit code -1
        /// </summary>
        public static ErrorHandlerDelegate ErrorHandler { get; set; } = ExitProccessErrorHandler;

        protected static void Error(string message, string faultyArgument) => ErrorHandler?.Invoke(message, faultyArgument);

        /// <summary> Lazy value that is implicitly converted to target type when used </summary>
        public class LazyImplicit<T> : Lazy<T>
        {
            public LazyImplicit(Func<T> valueFactory) : base(valueFactory) { }
            public static implicit operator T(LazyImplicit<T> lazy) => lazy.Value;
        }
    }
}
