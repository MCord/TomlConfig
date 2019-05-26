namespace TomlConfiguration
{
    using System;

    public class TomlConfigurationException : Exception
    {
        public TomlConfigurationException(string message) : base(message)
        {
        }

        public TomlConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}