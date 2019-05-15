namespace TomlConfig
{
    using System;
    using System.IO;

    public static class TomlConfig
    {
        public static T Read<T>(Stream data) => new TomlConfigReader().Read<T>(data);
        public static object Read(Type type,Stream data) => new TomlConfigReader().Read(type, data);
        public static T ReadWithDefault<T>(Stream data, T @default) => new TomlConfigReader().ReadWithDefault(data,@default);
    }
}