namespace Tests
{
    using Jokedst.GetOpt;

    using NUnit.Framework;

    [TestFixture]
    public class CommandLineOptionTests
    {
        [Test]
        public void Constructor1Works()
        {
            // Arrange
            int t = 1;

            // Act
            var clo = new CommandLineOption('a', "abc", "desc", ParameterType.None, value => t++);
            clo.SetFunction(null);

            // Assert
            Assert.AreEqual("desc", clo.Description);
            Assert.AreEqual("abc", clo.LongName);
            Assert.AreEqual('a', clo.ShortName);
            Assert.AreEqual(ParameterType.None, clo.ParameterType);
            Assert.AreEqual(2, t);
        }

        [Test]
        public void Constructor2Works()
        {
            // Arrange
            int t = 1;

            // Act
            var clo = new CommandLineOption("desc", ParameterType.Integer, value => t++, true);
            clo.SetFunction(null);

            // Assert
            Assert.AreEqual("desc", clo.Description);
            Assert.AreEqual(null, clo.LongName);
            Assert.AreEqual('\0', clo.ShortName);
            Assert.AreEqual(ParameterType.Integer, clo.ParameterType);
            Assert.AreEqual(2, t);
        }
    }
}
