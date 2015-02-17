namespace Tests
{
    using System;
    using System.IO;

    using Jokedst.GetOpt;

    using NUnit.Framework;

    [TestFixture]
    public class GetOptsTests
    {
        private StringWriter stdOut;
        private TextWriter originalOutput;

        private string[] StdOutLines
        {
            get
            {
                return this.stdOut.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            }
        }

        [SetUp]
        public void Setup()
        {
            // Redirect stdout so we can compare it in tests
            this.stdOut = new StringWriter();
            this.originalOutput = Console.Out;
            Console.SetOut(this.stdOut);
        }

        [TearDown]
        public void TearDown()
        {
            // Restore stdout
            Console.SetOut(this.originalOutput);
            this.stdOut.Dispose();
        }

        [Test]
        public void CanRedirectConsole()
        {
            Console.WriteLine("hello");
            Assert.AreEqual("hello" + Environment.NewLine, this.stdOut.ToString());
        }

        [Test]
        public void CanParseShortParameter()
        {
            bool a = false;
            var opts = new GetOpt("desc", new[] { new CommandLineOption('a', null, "a param", ParameterType.None, o => a = true), });

            // Act
            opts.ParseOptions(new[] { "foo", "-a", "bar" });

            // Assert
            Assert.AreEqual(true, a);
        }

        [Test]
        public void CanParseMultipleShortParametersInShortForm()
        {
            bool a = false, b = true, c = false;
            var opts = new GetOpt("desc",
                new[]
                    {
                        new CommandLineOption('a', null, "a param", ParameterType.None, o => a = true),
                        new CommandLineOption('b', null, "a param", ParameterType.None, o => b = false),
                        new CommandLineOption('c', null, "a param", ParameterType.None, o => c = true),
                    });

            // Act
            opts.ParseOptions(new[] { "foo", "-abc", "bar" });

            // Assert
            Assert.AreEqual(true, a);
            Assert.AreEqual(false, b);
            Assert.AreEqual(true, c);
        }

        [Test]
        public void CanParseMultipleShortParametersSeparated()
        {
            bool a = false, b = true, c = false;
            var opts = new GetOpt("desc",
                new[]
                    {
                        new CommandLineOption('a', null, "a param", ParameterType.None, o => a = true),
                        new CommandLineOption('b', null, "a param", ParameterType.None, o => b = false),
                        new CommandLineOption('c', null, "a param", ParameterType.None, o => c = true),
                    });

            // Act
            opts.ParseOptions(new[] { "foo", "-a", "bar", "-b", "-c" });

            // Assert
            Assert.AreEqual(true, a);
            Assert.AreEqual(false, b);
            Assert.AreEqual(true, c);
        }

        [Test]
        public void CanShowHelp()
        {
            // Arrange
            bool a = false, b = true, c = false;
            var opts = new GetOpt("desc",
                new[]
                    {
                        new CommandLineOption('a', null, "a param", ParameterType.None, null),
                        new CommandLineOption('b', "btext", "a param", ParameterType.None, null),
                        new CommandLineOption('c', null, "a param", ParameterType.None, null),
                    });

            // Act
            opts.ShowUsage(false);

            // Assert
            var output = this.StdOutLines;
            Assert.AreEqual(8, output.Length);
            Assert.AreEqual("desc", output[0]);
            Assert.IsTrue(output[1].StartsWith("Usage: "));
            Assert.IsTrue(output[1].EndsWith(" -abch"));
        }

        [Test]
        [ExpectedException(typeof(CommandLineException), ExpectedMessage = "Missing parameters")]
        public void StringParamPlusMissingFilenameShouldGenerateError()
        {
            string s = null, f = null;

            var opts = new GetOpt("desc",
                new[]
                {
                    new CommandLineOption('s', null, "a param", ParameterType.String, o => s = (string)o),
                    new CommandLineOption("filename", ParameterType.String, o => f = (string)o),
                });

            // Act
            opts.ParseOptions(new[] { "-s", "sparam" });
        }
    }
}