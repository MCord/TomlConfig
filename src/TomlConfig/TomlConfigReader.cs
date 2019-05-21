namespace TomlConfig
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Tomlyn;
    using Tomlyn.Model;

    public class TomlConfigReader
    {
        private readonly List<ITypeConverter> converters = new List<ITypeConverter>();

        public void AddTypeConverter(in ITypeConverter converter)
        {
            converters.Add(converter);
        }
        
        /// <summary>
        ///     This delegate is called when the object is hydrated.
        ///     <param>The first param is the object after values are set.</param>
        ///     <param>the second param is the source toml table.</param>
        /// </summary>
        public Action<object, TomlTable> OnTableParsingFinished;

        /// <summary>
        ///     This delegate is called when before a new object is hydrated.
        ///     <param>First argument is the instance being hydrated.</param>
        ///     <param>Second argument is the toml table containing data to be mapped to object.</param>
        ///     <param>Third object is the default object that would provide missing value when needed.</param>
        ///     <returns>
        ///         An instance of object that will be used as default, should have the same type as the instance being
        ///         hydrated.
        ///     </returns>
        /// </summary>
        public Func<object, TomlTable, object, object> OnTableParsingStarted;

        /// <summary>
        ///     Reads an stream containing a toml file and returns an object of type T
        /// </summary>
        /// <param name="data">The stream containing toml</param>
        /// <typeparam name="T">The type to be deserialized.</typeparam>
        /// <returns>An object of type T deserialized from file content.</returns>
        public T Read<T>(Stream data)
        {
            return (T) Read(typeof(T), data);
        }

        /// <summary>
        ///     Reads an stream containing a toml file and returns an object.
        /// </summary>
        /// <param name="t">object Type to be deserialized</param>
        /// <param name="data">The stream containing toml</param>
        /// <returns>An object deserialized from file content.</returns>
        public object Read(Type t, Stream data)
        {
            var tomlTable = Toml.Parse(new StreamReader(data).ReadToEnd());
            return ConvertTable(t, tomlTable.ToModel());
        }

        /// <summary>
        ///     This function can deserialize an object using default value from another object.
        /// </summary>
        /// <param name="type">The type of class to be deserialized.</param>
        /// <param name="data">The stream containing toml data.</param>
        /// <param name="default">An instance with the same type as the object being deserialized.</param>
        /// <returns>A deserialized object</returns>
        /// <remarks>
        ///     If a property for the instance is not specified in the stream, the value from the default instance will be set
        ///     on the object.
        /// </remarks>
        public object ReadWithDefault(Type type, Stream data, object @default)
        {
            var tomlTable = Toml.Parse(new StreamReader(data).ReadToEnd());
            return ConvertTable(type, tomlTable.ToModel(), @default);
        }

        /// <summary>
        ///     This function can deserialize an object using default value from another object.
        /// </summary>
        /// <param name="data">The stream containing toml data.</param>
        /// <param name="default">An instance with the same type as the object being deserialized.</param>
        /// <typeparam name="T">The type of the object to be deserialized.</typeparam>
        /// <returns>A deserialized object</returns>
        /// <remarks>
        ///     If a property for the instance is not specified in the stream, the value from the default instance will be set
        ///     on the object.
        /// </remarks>
        public T ReadWithDefault<T>(Stream data, T @default)
        {
            return (T) ReadWithDefault(typeof(T), data, @default);
        }

        private object ConvertTable(Type t, TomlTable tomlTable, object @default = null)
        {
            var instance = Activator.CreateInstance(t);
            var overrideDefault = OnTableParsingStarted?.Invoke(instance, tomlTable, @default);
            @default = overrideDefault ?? @default;

            var properties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();

            foreach (var key in tomlTable.Keys)
            {
                var match = properties.FirstOrDefault(x => x.Name == key);

                if (match == null)
                {
                    throw new TomlConfigurationException(
                        $"No public instance property named '{key}' is found on '{t.FullName}'");
                }

                properties.Remove(match);
                try
                {
                    match.SetValue(instance, ConvertToType(match.PropertyType, tomlTable[key], match));
                }
                catch (Exception ex)
                {
                    throw new TomlConfigurationException(
                        $"Unable to convert value '{tomlTable[key]}' to type {match.PropertyType} from key '{key}'"
                        , ex);
                }
            }

            if (@default != null)
            {
                foreach (var unspecifiedProperty in properties)
                {
                    unspecifiedProperty.SetValue(instance, unspecifiedProperty.GetValue(@default));
                }
            }

            OnTableParsingFinished?.Invoke(instance, tomlTable);
            return instance;
        }

        private object ConvertToType(Type targetType, object value, PropertyInfo propInfo)
        {
            switch (value)
            {
                case TomlValue v:
                    return Convert(v.ValueAsObject, targetType, propInfo);
                case TomlArray array:
                    return ConvertValueArray(array.GetTomlEnumerator(), targetType);
                case TomlTableArray tableArray:
                    return ConvertValueArray(tableArray, targetType);
                case TomlTable table:
                    return ConvertTable(targetType, table);
                default:
                    return Convert(value, targetType, propInfo);
            }
        }

        private object Convert(object value, Type t, PropertyInfo propInfo)
        {
            foreach (var cnv in converters)
            {
                if (cnv.CanConvert(t, propInfo))
                {
                    return cnv.Convert(value, t);
                }
            }
            
            return System.Convert.ChangeType(value, t);
        }

        private object ConvertValueArray(IEnumerable<TomlObject> items, Type targetType)
        {

            if (targetType.IsArray)

            {
                var elementType = targetType.GetElementType();

                if (elementType == null)
                {
                    var join = string.Join(",", items.Select(x => x.ToString()));

                    throw new TomlConfigurationException($"array value [{join}] can not be cast to type {targetType}" +
                                                         " because it's not an array.");
                }

                var converted = items
                    .Select(x => ConvertToType(elementType, x, null)).ToArray();
                var result = Array.CreateInstance(elementType, converted.Length);
                converted.CopyTo(result, 0);
                return result;
            }

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var result = (IList)Activator.CreateInstance(targetType);

                foreach (var converted in items.Select(x => ConvertToType(targetType.GetGenericArguments()[0], x, null)))
                {
                    result.Add(converted);
                }
                
                return result;
            }
            
            throw new TomlConfigurationException(
                $"Conversion from toml array to {targetType.FullName} is not supported. " +
                $"Please use an array or a generic List.");
        }
    }
}