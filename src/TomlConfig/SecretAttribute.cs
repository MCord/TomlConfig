namespace TomlConfiguration
{
    using System;

    /// <summary>
    /// This attribute marks any property that contains secret data. This indication is used
    /// by the parser to decrypt the secrets when deserializing configuration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SecretAttribute : Attribute
    {
    }
}