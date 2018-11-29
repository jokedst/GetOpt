namespace Okedst.Args
{

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// A no-configuration command line parameter parser, following getopt standard.
    /// </summary>
    /// <remarks>
    /// Flag : an option without parameters, e.g. "-v" or each letter in "-xkc"
    /// Parameter : an option with a parameter, e.g. "-f file.txt" or "-C:Debug"
    /// Argument : an option not linked to a letter at all, e.g. in "cp file1 file2" file1 and file2 are arguments
    /// </remarks>
    public class Args
    {
        private class DetectedUsage
        {
            public char? ShortName { get; }
            public string LongName { get; }
            public string Description { get; }
            public virtual object Value { get; }

            public DetectedUsage(char? shortName, string longName, string description)
            {
                this.ShortName = shortName;
                this.LongName = longName;
                this.Description = description;
            }
        }

        private class DetectedFlag : DetectedUsage
        {
            public DetectedFlag(char? shortName, string longName, string description, bool isSet) 
                : base(shortName, longName, description)
            {
                this.IsSet = isSet;
            }

            public bool IsSet { get; }
            public override object Value => this.IsSet;
        }
        private static readonly List<DetectedUsage> DetectedUsages = new List<DetectedUsage>();
        private static readonly List<string> Arguments;
        private static readonly List<string> PureArguments = new List<string>();
        private static readonly string ExeFileName;
        private static readonly HashSet<char> FlagCache = new HashSet<char>();
        private static readonly HashSet<string> FlagStringCache = new HashSet<string>();

        static Args()
        {
            Arguments = Environment.GetCommandLineArgs().ToList();
            ExeFileName = Arguments[0];
            Arguments.RemoveAt(0);
            InitFlagCache();
        }

        /// <summary>
        /// Set arguments manually, replacing values loaded from command line
        /// </summary>
        /// <param name="arguments"> Arguments to parse </param>
        public static void SetArguments(params string[] arguments)
        {
            Arguments.Clear();
            Arguments.AddRange(arguments);
            InitFlagCache();
        }

        private static void InitFlagCache()
        {
            DetectedUsages.Clear();
            PureArguments.Clear();
            FlagStringCache.Clear();
            FlagCache.Clear();
            var doubleDash = Arguments.IndexOf("--");
            if (doubleDash != -1)
            {
                Arguments.RemoveAt(doubleDash);
                if (Arguments.Count > doubleDash)
                {
                    PureArguments.AddRange(Arguments.Skip(doubleDash));
                }
                Arguments.RemoveRange(doubleDash, PureArguments.Count);
            }
            // Find all "multi-flag" options ("-acHe") and parse them
            var index = Arguments.FindIndex(a => a.Length > 2 && a[0] == '-' && a[1] != '-');
            while (index != -1)
            {
                for (int i = 1; i < Arguments[index].Length; i++)
                {
                    FlagCache.Add(Arguments[index][i]);
                }
                Arguments.RemoveAt(index);
                index = Arguments.FindIndex(a => a.Length > 2 && a[0] == '-' && a[1] != '-');
            }
        }

        /// <summary>
        /// Get next program argument
        /// </summary>
        /// <returns> Lazy-loaded value that is evaluated when cast to string </returns>
        public static LazyImplicit<string> Next()
        {
            // If parameters look like "-f hello" we don't know if the "hello" belongs to the "-f" or not
            // until that flag is used. So we postpone the evaluation of this as long as possible
            return new LazyImplicit<string>(() =>
            {
                var lastWasFlag = false;
                for (var i = 0; i < Arguments.Count; i++)
                {
                    var arg = Arguments[i];
                    if (arg.StartsWith("-"))
                    {
                        lastWasFlag = true;
                    }
                    else if (lastWasFlag)
                    {
                        Error("Error: ambiguous parameter order", Arguments[i]);
                    }
                    else
                    {
                        Arguments.RemoveAt(i);
                        return arg;
                    }
                }

                if (PureArguments.Any())
                {
                    var x = PureArguments[0];
                    PureArguments.RemoveAt(0);
                    return x;
                }

                Error("Argument missing", "");
                return null;
            });
        }

        public static bool Flag(char flagChar)
        {
            if (DetectedUsages.TryFind(x => x.ShortName == flagChar, out DetectedFlag found))
                return found.IsSet;

            if (FlagCache.Contains(flagChar)) return true;
            var index = Arguments.IndexOf("-" + flagChar);
            DetectedUsages.Add(new DetectedFlag(flagChar, null, null, index != -1));
            if (index != -1)
            {
                Arguments.RemoveAt(index);
                FlagCache.Add(flagChar);
                return true;
            }

            return false;
        }

        public static bool Flag(string flagName)
        {
            if (DetectedUsages.TryFind(x => x.LongName == flagName, out DetectedFlag found))
                return found.IsSet;

            var index = Arguments.IndexOf("--" + flagName);
            DetectedUsages.Add(new DetectedFlag(null, flagName, null, index != -1));
            if (index != -1)
            {
                Arguments.RemoveAt(index);
                FlagStringCache.Add(flagName);
                return true;
            }

            return false;
        }

        public static bool Flag(char flagChar, string flagName)
        {
            DetectedFlag foundLong = null;
            if (DetectedUsages.TryFind(x => x.ShortName == flagChar, out DetectedFlag foundShort)
                || DetectedUsages.TryFind(x => x.LongName == flagName, out foundLong))
            {
                if (foundShort != null && foundLong != null && foundShort != foundLong)
                {
                    // TODO: Merge these two if possible
                }

                return (foundShort?.IsSet ?? false) || foundLong.IsSet;
            }

            var index = Arguments.FindIndex(x => x == "-" + flagChar || x == "--" + flagName);
            DetectedUsages.Add(new DetectedFlag(flagChar, flagName, null, index != -1));
            if (index != -1)
            {
                Arguments.RemoveAt(index);
                FlagStringCache.Add(flagName);
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
                        Arguments.RemoveRange(i, 2);
                        return result;
                    }
                }
            }

            return null;
        }

        public static T Get<T>(char parameterChar, T defaultValue = default(T))
        {
            var i = Arguments.IndexOf("-" + parameterChar);
            if (i == -1)
                return defaultValue;
            if (Arguments.Count <= i + 1)
                Error($"Missing parameter value after '-{parameterChar}'", Arguments[i]);

            var converter = TypeDescriptor.GetConverter(typeof(T));
            var ret = (T)converter.ConvertFromString(Arguments[i + 1]);

            Arguments.RemoveRange(i, 2);
            return ret;
        }


        public static void SetErrorHandler(Action<string> errorHandler)
        {
            ErrorHandler = (message, argument) => errorHandler(message);
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

        internal static class Extensions
        {
            public static bool TryFind<TBase,TDerived>(this List<TBase> items, Func<TDerived, bool> predicate, out TDerived result)
            where TDerived : TBase
            {
                result = items.OfType<TDerived>().FirstOrDefault(predicate);
                return result != null;
            }
        }
}
