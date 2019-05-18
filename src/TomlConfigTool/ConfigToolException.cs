namespace TomlConfigTool
{
    using System;

    public class ConfigToolException : Exception
    {
        public ConfigToolException(string message) : base(message)
        {
        }
    }
}