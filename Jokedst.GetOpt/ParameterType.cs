namespace Jokedst.GetOpt
{
    /// <summary>
    /// Supported parameter types.
    /// Everything else should be handled as strings and parsed by the client application
    /// </summary>
    public enum ParameterType
    {
        /// <summary> This parameter takes no arguments </summary>
        None,

        /// <summary> This parameter takes an integer as argument </summary>
        Integer,

        /// <summary> This parameter takes a string as argument </summary>
        String,

        /// <summary> This parameter takes a double as argument </summary>
        Double
    }
}