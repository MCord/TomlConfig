namespace TomlConfig
{
    using System;

    public interface ITypeConverter
    {
        bool CanConvert(Type t);
        object Convert(object instance, Type type);
    }
}