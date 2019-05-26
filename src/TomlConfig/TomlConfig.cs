[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Test")]

namespace TomlConfiguration
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public static class TomlConfig
    {
        public static TomlConfigSettings FromStream(Stream data, string pathHint = null)
        {
            var tomlConfigSettings = TomlConfigSettings.Default;
            tomlConfigSettings.Data = new NamedStream(pathHint, data);
            return tomlConfigSettings;
        }
        
        public static TomlConfigSettings FromString(string data, string pathHint = null)
        {
            var tomlConfigSettings = TomlConfigSettings.Default;
            tomlConfigSettings.Data = new NamedStream(pathHint, new MemoryStream(Encoding. UTF8.GetBytes(data)));
            return tomlConfigSettings;
        }
        
        public static TomlConfigSettings FromFile(string filePath)
        {
            var tomlConfigSettings = TomlConfigSettings.Default;
            tomlConfigSettings.Data = new NamedStream(filePath, File.Open(filePath, FileMode.Open));
            return tomlConfigSettings;
        }

        public static TomlConfigSettings WithCustomTypeConverter(this TomlConfigSettings settings,
            ITypeConverter converter)
        {
            settings.CustomTypeConverters.Add(converter);
            return settings;
        }
        
        public static TomlConfigSettings WithMasterKey(this TomlConfigSettings settings,
            string masterKey)
        {
            settings.CustomTypeConverters.Insert(0, new PasswordTypeConverter(new SecretKeeper(masterKey)));
            return settings;
        }
        
        public static TomlConfigSettings WithOverrides(this TomlConfigSettings settings,
            Dictionary<string, string> overrides)
        {
            settings.Overrides = overrides;
            return settings;
        }
        
        public static TomlConfigSettings WithOverride(this TomlConfigSettings settings,
            string propertyName, string value)
        {
            settings.Overrides[propertyName] = value;
            return settings;
        }

        public static T Read<T>(this TomlConfigSettings settings)
        {
            var reader = new TomlConfigReader(settings);
            return (T) reader
                .Read<T>(settings.Data.Stream, settings.Data.Path)
                .WithOverrides<T>(settings.Overrides);
        }
        
        public static T ReadWithDefault<T>(this TomlConfigSettings settings, T defaultInstance)
        {
            var reader = new TomlConfigReader(settings);
            return (T) reader
                .ReadWithDefault(settings.Data.Stream, defaultInstance)
                .WithOverrides<T>(settings.Overrides);
        }
    }
}