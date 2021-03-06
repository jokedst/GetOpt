﻿namespace Tests
{
    using System.IO;
    using Okedst.Args;
    using NUnit.Framework;
    using System;

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
            var param = Args.Get('a');
            Assert.AreEqual("maybe parameter", param);
            Assert.AreEqual("argument",(string) arg);
        }

        [Test]
        public void Can_read_multiple_parameters_of_same_type()
        {
            Args.SetArguments("-f", "file1.txt", "-f", "file2.txt");

            var file1 = Args.Get('f');
            var file2 = Args.Get('f');

            Assert.AreEqual("file1.txt", file1);
            Assert.AreEqual("file2.txt", file2);
        }

        [Test]
        public void Multiple_parameters_are_returned_in_the_right_order()
        {
            Args.SetArguments("--file", "file1.txt", "-f", "file2.txt");
            Assert.AreEqual("file1.txt", Args.GetLong('f', "file"));
            Assert.AreEqual("file2.txt", Args.GetLong('f', "file"));

            Args.SetArguments("-f", "file1.txt", "--file", "file2.txt");
            Assert.AreEqual("file1.txt", Args.GetLong('f', "file"));
            Assert.AreEqual("file2.txt", Args.GetLong('f', "file"));
        }

        [Test]
        public void Can_read_multiple_parameters_of_same_type_as_array()
        {
            Args.SetArguments("-f", "file1.txt", "-f", "file2.txt");

            var files = Args.GetAll('f');

            Assert.AreEqual(2, files.Count);
            Assert.AreEqual("file1.txt", files[0]);
            Assert.AreEqual("file2.txt", files[1]);
        }

        [Test]
        public void ShotgunTest()
        {
            Args.SetArguments("file1.txt", "-a", "aparam", "-wsd", "file2.txt");

            Assert.IsFalse(Args.Flag('f'));
            Assert.IsTrue(Args.Flag('s'));
            Assert.IsTrue(Args.Flag('w'));
            Assert.AreEqual("aparam", Args.Get('a'));
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

            var p = Args.Get('p');

            Assert.Null(p);
        }

        [Test]
        public void Can_get()
        {
            Args.SetArguments("Hello", "-p", "1500");
            var p = Args.Get<int>('p');
            Assert.AreEqual(1500, p);

            Args.SetArguments("Hello", "-p", "1500");
            p = Args.Get('p', 0);
            Assert.AreEqual(1500, p);

            Args.SetArguments("Hello", "-p", "1500");
            p = Args.Get('x', 600);
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

        [Test]
        public void Can_get_default_values()
        {
            Args.SetArguments();
            var arg1 = Args.Next("first");
            var arg2 = Args.Next("second");

            Assert.AreEqual("first", (string)arg1);
            Assert.AreEqual("second", (string)arg2);
        }

        [Test, SetCulture("en-us")]
        public void Can_convert_values()
        {
            Args.SetArguments("--int", "345", "-l", "23.87");
            var arg1 = Args.GetLong('i', "int", 9);
            var arg2 = Args.Get('l', 0.0);

            Assert.AreEqual(345, arg1);
            Assert.AreEqual(23.87, arg2);
        }

        [Test]
        public void Can_transform_parameters()
        {
            Args.SetArguments("-o","file.txt");
            var arg1 = Args.Get('o', "DEFAULT", o => o.ToUpper());

            Assert.AreEqual("FILE.TXT", arg1);
        }

        [Test]
        public void Can_generate_help()
        {
            Args.SetArguments();
            Args.ExeFileName = "Test";
            Args.GetLong("param");
            Args.Get('p');
            Args.Next("hello");
            Args.Flag('f');
            Args.Flag("flag");

            var helpText = Args.GenerateHelp();
            string first = new StringReader(helpText).ReadLine();
            Console.WriteLine(helpText);

            Assert.AreEqual("Test -f --flag -p <String> --param <String> [argument]", first);
        }

        [Test]
        public void Help_groups_short_flags()
        {
            Args.SetArguments();
            Args.ExeFileName = "Test";
            Args.Flag('c');
            Args.Flag('b');
            Args.Flag('a');

            var helpText = Args.GenerateHelp();
            string first = new StringReader(helpText).ReadLine();

            Assert.AreEqual("Test -cba", first);
        }
    }
}
