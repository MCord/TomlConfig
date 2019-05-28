namespace TomlConfiguration
{
    using System;
    using System.Collections.Generic;

    public static class TypeInfo
    {
        public static bool IsGenericList(this Type targetType)
        {
            return targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>);
        }

        public static bool IsGenericDictionary(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }
    }
}