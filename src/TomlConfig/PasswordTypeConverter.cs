namespace TomlConfiguration
{
    using System;
    using System.Linq;
    using System.Reflection;

    public class PasswordTypeConverter : ITypeConverter
    {
        private readonly SecretKeeper keeper;

        public PasswordTypeConverter(SecretKeeper keeper)
        {
            this.keeper = keeper;
        }

        public bool CanConvert(Type t, Attribute[] metaData)
        {
            return (metaData?.Cast<SecretAttribute>().Any()).GetValueOrDefault();
        }

        public object Convert(object instance, Type type)
        {
            if (instance == null || string.IsNullOrWhiteSpace(instance.ToString()))
            {
                return instance;
            }

            if (!keeper.IsValidCypher(instance.ToString(), out _, out _))
            {
                throw new TomlConfigurationException(
                    $"The value '{instance}' specified is not a valid secret and can not be decrypted.");
            }

            return keeper.Decrypt(instance.ToString());
        }
    }
}