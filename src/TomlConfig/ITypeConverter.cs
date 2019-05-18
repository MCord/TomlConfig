namespace TomlConfig
{
    using System;
    using System.Reflection;

    public interface ITypeConverter
    {
        bool CanConvert(Type t, PropertyInfo info);
        object Convert(object instance, Type type);
    }
}