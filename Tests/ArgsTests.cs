namespace Tests
{
    using Okedst.Args;
    using NUnit.Framework;

    public class ArgsTests
    {
        [Test]
        public void Can_parse_flag()
        {
            Args.SetArguments("-f");
            var hasF = Args.Flag('f');
            Assert.IsTrue(hasF);
        }

        [Test]
        public void Can_parse_multiflags()
        {
            Args.SetArguments("-afb");
            var hasF = Args.Flag('f');
            Assert.IsTrue(hasF);
        }

        [Test]
        public void Can_parse_long_flag()
        {
            Args.SetArguments("--verbose");
            var hasF = Args.Flag("verbose");
            Assert.IsTrue(hasF);
        }

        [Test]
        public void Can_parse_flag_both_short_and_long()
        {
            Args.SetArguments("--verbose", "-x");
            Assert.IsTrue(Args.Flag('v', "verbose"));
            Assert.IsTrue(Args.Flag('x', "xor"));
            Assert.IsFalse(Args.Flag('o', "org"));
        }

        [Test]
        public void Returns_false_for_missing_flag()
        {
            Args.SetArguments("-a");
            var hasF = Args.Flag('f');
            Assert.IsFalse(hasF);
        }

        [Test]
        public void Ambigous_options_flagpath()
        {
            Args.SetArguments("-a", "maybe parameter", "argument");
            var arg = Args.Next();
            // value of 'arg' depends on if 'a' is a flag or parameter, which we don't know yet

            // We chose 'flag'
            var hasF = Args.Flag('a');
            Assert.IsTrue(hasF);
            Assert.AreEqual("maybe parameter", (string)arg);
        }

        [Test]
        public void Ambigous_options_parameterpath()
        {
            Args.SetArguments("-a", "maybe parameter", "argument");
            var arg = Args.Next();
            // value of 'arg' depends on if 'a' is a flag or parameter, which we don't know yet

            // We chose 'parameter'
            var param = Args.Parameter('a');
            Assert.AreEqual("maybe parameter", param);
            Assert.AreEqual("argument",(string) arg);
        }

        [Test]
        public void ShotgunTest()
        {
            Args.SetArguments("file1.txt", "-a", "aparam", "-wsd", "file2.txt");

            Assert.IsFalse(Args.Flag('f'));
            Assert.IsTrue(Args.Flag('s'));
            Assert.IsTrue(Args.Flag('w'));
            Assert.AreEqual("aparam", Args.Parameter('a'));
            Assert.AreEqual("file1.txt", (string)Args.Next());
            Assert.AreEqual("file2.txt", (string)Args.Next());
        }

        [Test]
        public void Double_slash_ends_flagparsing()
        {
            Args.SetArguments("-a","--","-b");
            Assert.IsFalse(Args.Flag('b'));
            Assert.IsTrue(Args.Flag('a'));
            Assert.AreEqual("-b", (string)Args.Next());
        }

        [Test]
        public void Missing_parameter()
        {
            Args.SetArguments("Hello");

            var p = Args.Parameter('p');

            Assert.Null(p);
        }

        [Test]
        public void Can_get()
        {
            Args.SetArguments("Hello","-p","1500");
            var p = Args.Get<int>('p');
            Assert.AreEqual(1500, p);

            Args.SetArguments("Hello", "-p", "1500");
            p = Args.Get('p',0);
            Assert.AreEqual(1500, p);

            Args.SetArguments("Hello", "-p", "1500");
            p = Args.Get('x',600);
            Assert.AreEqual(600, p);
        }


        [Test]
        public void Can_get_flag_everal_times()
        {
            Args.SetArguments("-f");
            Assert.IsTrue(Args.Flag('f'));
            Assert.IsTrue(Args.Flag('f'));
            Assert.IsTrue(Args.Flag('f'));
        }
    }
}
