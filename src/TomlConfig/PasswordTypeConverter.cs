namespace TomlConfig
{
    using System;
    using System.Reflection;

    public class PasswordTypeConverter : ITypeConverter
    {
        private readonly SecretKeeper keeper;

        public PasswordTypeConverter(SecretKeeper keeper)
        {
            this.keeper = keeper;
        }

        public bool CanConvert(Type t, PropertyInfo info)
        {
            return info?.GetCustomAttribute<SecretAttribute>() != null;
        }

        public object Convert(object instance, Type type)
        {
            if (instance == null || !keeper.IsValidCypher(instance.ToString(), out _ , out _))
            {
                return instance;
            }
            
            return keeper.Decrypt(instance.ToString());
        }
    }
}