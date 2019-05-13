namespace TomlConfig
{
    using System;

    public class TomlConfigurationException : System.Exception
    {
        public TomlConfigurationException(string message) : base(message)
        {
        }

        public TomlConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}