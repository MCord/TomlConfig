namespace TomlConfiguration
{
    using System;
    using System.Reflection;

    public static class TypeConverter 
    {
        private class MethodTypeConverter<T> : ITypeConverter
        {
            private readonly Func<Type, object, T> conversion;

            public MethodTypeConverter(Func<Type, object, T> conversion)
            {
                this.conversion = conversion;
            }

            public bool CanConvert(Type t, Attribute[] _)
            {
                return t == typeof(T);
            }

            public object Convert(object instance, Type type, object parent)
            {
                return conversion(type, instance);
            }
        }
        
        public static ITypeConverter From<T>(Func<Type, object, T> method)
        {
            return new MethodTypeConverter<T>(method);
        }
    }
}