namespace TomlConfig
{
    using System.Collections.Generic;

    public class TomlConfigSettings
    {
        public Dictionary<string, string> Overrides { get; set; }
        public List<ITypeConverter> CustomTypeConverters { get; set; }

        public static readonly TomlConfigSettings Default = new TomlConfigSettings
        {
            Overrides = new Dictionary<string, string>(),
            CustomTypeConverters = new List<ITypeConverter>()
            {
                new PasswordTypeConverter(SecretKeeper.Default)
            }
        };
    }
}