namespace Jokedst.GetOpt
{
    /// <summary>
    /// Describes a possible option that will be parsed from the command line parameters
    /// </summary>
    public class CommandLineOption
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineOption"/> class.
        /// </summary>
        /// <param name="shortName"> Single character name of this parameter. Optional. </param>
        /// <param name="longName"> The long name of this parameter. Optional. </param>
        /// <param name="description"> Description of this parameter, to be used in the Usings function. </param>
        /// <param name="parameterType"> Type of parameter </param>
        /// <param name="setFunction"> Function to call when this parameter is found. Function parameter is found value </param>
        public CommandLineOption(char shortName, string longName, string description, ParameterType parameterType, SetOptionDelegate setFunction)
        {
            this.ShortName = shortName;
            this.LongName = longName;
            this.Description = description;
            this.ParameterType = parameterType;
            this.SetFunction = setFunction;
            this.IsOptional = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineOption"/> class.
        /// This constructor is used to create an unnamed parameter
        /// </summary>
        /// <param name="description"> Description of this parameter, to be used in the Usings function. </param>
        /// <param name="parameterType"> Type of parameter </param>
        /// <param name="setFunction"> Function to call when this parameter is found. Function parameter is found value </param>
        /// <param name="optional"> If false an exception will be thrown if this parameter can not be found. </param>
        public CommandLineOption(string description, ParameterType parameterType, SetOptionDelegate setFunction, bool optional = false)
        {
            this.ShortName = '\0';
            this.LongName = null;
            this.Description = description;
            this.ParameterType = parameterType;
            this.SetFunction = setFunction;
            this.IsOptional = optional;
        }

        /// <summary>Gets single character version of this option (e.g. -f)</summary>
        public char ShortName { get; private set; }

        /// <summary>Gets long version of this option (e.g. --file)</summary>
        public string LongName { get; private set; }

        /// <summary>Gets description that will be shown in the usage. Also the (optional) name of a unnamed parameter</summary>
        public string Description { get; private set; }

        /// <summary>Gets type of value this parameter accepts</summary>
        public ParameterType ParameterType { get; private set; }

        /// <summary>Gets function that will be called when this option has been parsed</summary>
        public SetOptionDelegate SetFunction { get; private set; }

        /// <summary>Gets a value indicating whether an unnamed parameter is optional</summary>
        internal bool IsOptional { get; private set; }
        
        /// <summary>Gets the name of this option, regardless of what type it is</summary>
        internal string Name
        {
            get
            {
                if (this.LongName != null)
                {
                    return this.LongName;
                }

                if (this.ShortName != '\0')
                {
                    return "-" + this.ShortName;
                }

                return this.Description;
            }
        }
    }
}