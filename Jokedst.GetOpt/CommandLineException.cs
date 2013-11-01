namespace Jokedst.GetOpt
{
    using System;

    /// <summary>
    /// A command line option parsing exception.
    /// </summary>
    public class CommandLineException : Exception
    {
        /// <summary> 
        /// Initializes a new instance of the <see cref="CommandLineException"/> class.
        /// </summary>
        /// <param name="message"> The message that describes the error. </param>
        public CommandLineException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineException"/> class.
        /// </summary>
        /// <param name="message"> The message that describes the error. </param>
        /// <param name="parameter"> The parameter with the error. </param>
        public CommandLineException(string message, string parameter) : base(message + "\n Parameter " + parameter)
        {
        }
    }
}