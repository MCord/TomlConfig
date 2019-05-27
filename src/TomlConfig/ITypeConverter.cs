namespace TomlConfiguration
{
    using System;

    public interface ITypeConverter
    {
        bool CanConvert(Type t, Attribute[] metaData);
        object Convert(object instance, Type type);
    }
}